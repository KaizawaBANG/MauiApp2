using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace MauiApp2.Services
{
    public class AutoSyncService : IAutoSyncService
    {
        private readonly IConnectivityService _connectivityService;
        private readonly IDatabaseSyncService _databaseSyncService;
        private readonly ISyncQueueService _syncQueueService;
        
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isSyncing = false;
        private bool _isEnabled = true;
        private DateTime? _lastSyncTime = null;
        private SyncResult? _lastSyncResult = null;

        public bool IsSyncing => _isSyncing;
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set 
            { 
                _isEnabled = value;
                if (!_isEnabled && _cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }
        public DateTime? LastSyncTime => _lastSyncTime;
        public SyncResult? LastSyncResult => _lastSyncResult;

        public AutoSyncService(
            IConnectivityService connectivityService,
            IDatabaseSyncService databaseSyncService,
            ISyncQueueService syncQueueService)
        {
            _connectivityService = connectivityService;
            _databaseSyncService = databaseSyncService;
            _syncQueueService = syncQueueService;
            
            // Start monitoring when service is created
            _ = StartAsync();
        }

        public async Task StartAsync()
        {
            if (_cancellationTokenSource != null)
            {
                return; // Already running
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () => await MonitorAndSyncAsync(_cancellationTokenSource.Token));
        }

        public Task StopAsync()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            return Task.CompletedTask;
        }

        public async Task<bool> SyncNowAsync(bool force = false)
        {
            if (_isSyncing && !force)
            {
                return false; // Already syncing
            }

            try
            {
                _isSyncing = true;

                var localConnString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
                var cloudConnString = ConfigurationManager.ConnectionStrings["CloudConnection"]?.ConnectionString;

                if (string.IsNullOrEmpty(localConnString) || string.IsNullOrEmpty(cloudConnString))
                {
                    return false;
                }

                // Check connectivity first
                var isOnline = await _connectivityService.CheckCloudConnectivityAsync();
                if (!isOnline)
                {
                    return false;
                }

                // Perform full database sync
                _lastSyncResult = await _databaseSyncService.SyncDatabaseAsync(localConnString, cloudConnString);
                _lastSyncTime = DateTime.Now;

                // Process sync queue items
                await ProcessSyncQueueAsync();

                return _lastSyncResult.IsSuccess;
            }
            catch (Exception ex)
            {
                _lastSyncResult = new SyncResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
                return false;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private async Task MonitorAndSyncAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isEnabled)
            {
                try
                {
                    // Check connectivity every 30 seconds
                    await Task.Delay(30000, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Check if online
                    var isOnline = await _connectivityService.CheckCloudConnectivityAsync();
                    
                    if (isOnline)
                    {
                        // Check if there are pending items or if it's been a while since last sync
                        var pendingCount = await _syncQueueService.GetPendingCountAsync();
                        var shouldSync = pendingCount > 0 || 
                                        !_lastSyncTime.HasValue || 
                                        (DateTime.Now - _lastSyncTime.Value).TotalMinutes > 5;

                        if (shouldSync && !_isSyncing)
                        {
                            await SyncNowAsync();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Continue monitoring even if sync fails
                }
            }
        }

        private async Task ProcessSyncQueueAsync()
        {
            try
            {
                var pendingItems = await _syncQueueService.GetPendingItemsAsync(limit: 100);

                foreach (var item in pendingItems)
                {
                    try
                    {
                        await _syncQueueService.MarkAsSyncingAsync(item.QueueId);

                        // The full database sync should have already synced these records
                        // Mark as synced if the sync was successful
                        if (_lastSyncResult?.IsSuccess == true)
                        {
                            await _syncQueueService.MarkAsSyncedAsync(item.QueueId);
                        }
                        else
                        {
                            await _syncQueueService.MarkAsFailedAsync(item.QueueId, 
                                _lastSyncResult?.ErrorMessage ?? "Sync failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        await _syncQueueService.MarkAsFailedAsync(item.QueueId, ex.Message);
                    }
                }
            }
            catch
            {
                // Continue even if queue processing fails
            }
        }
    }
}

