using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MauiApp2.Services
{
    public class ConnectivityService : IConnectivityService
    {
        private bool _isOnline = false;
        private DateTime _lastCheckTime = DateTime.MinValue;
        private TimeSpan? _lastResponseTime = null;
        private string _lastError = string.Empty;

        public bool IsOnline => _isOnline;
        public DateTime LastCheckTime => _lastCheckTime;
        public TimeSpan? LastResponseTime => _lastResponseTime;
        public string LastError => _lastError;

        public event EventHandler<bool>? ConnectivityChanged;
        public event EventHandler<ConnectivityStatus>? StatusChanged;

        public async Task<bool> CheckCloudConnectivityAsync()
        {
            try
            {
                var cloudConnString = ConfigurationManager.ConnectionStrings["CloudConnection"]?.ConnectionString;
                if (string.IsNullOrEmpty(cloudConnString))
                {
                    _lastError = "Cloud connection string not configured";
                    UpdateStatus(false, null, _lastError);
                    return false;
                }

                var stopwatch = Stopwatch.StartNew();
                
                using var connection = new SqlConnection(cloudConnString);
                await connection.OpenAsync();
                
                // Test with a simple query
                using var cmd = new SqlCommand("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();
                
                stopwatch.Stop();
                _lastResponseTime = stopwatch.Elapsed;
                _lastCheckTime = DateTime.Now;
                _lastError = string.Empty;

                var wasOnline = _isOnline;
                _isOnline = true;

                if (wasOnline != _isOnline)
                {
                    ConnectivityChanged?.Invoke(this, _isOnline);
                }

                UpdateStatus(true, _lastResponseTime, null);
                return true;
            }
            catch (Exception ex)
            {
                _lastCheckTime = DateTime.Now;
                _lastError = ex.Message;
                _lastResponseTime = null;

                var wasOnline = _isOnline;
                _isOnline = false;

                if (wasOnline != _isOnline)
                {
                    ConnectivityChanged?.Invoke(this, _isOnline);
                }

                UpdateStatus(false, null, _lastError);
                return false;
            }
        }

        public async Task<bool> IsCloudAvailableAsync()
        {
            return await CheckCloudConnectivityAsync();
        }

        private void UpdateStatus(bool isOnline, TimeSpan? responseTime, string? errorMessage)
        {
            var status = new ConnectivityStatus
            {
                IsOnline = isOnline,
                CheckTime = _lastCheckTime,
                ResponseTime = responseTime,
                ErrorMessage = errorMessage ?? string.Empty
            };

            StatusChanged?.Invoke(this, status);

            // Log to database (optional, can be done in background)
            _ = LogConnectivityAsync(isOnline, responseTime, errorMessage);
        }

        private async Task LogConnectivityAsync(bool isOnline, TimeSpan? responseTime, string? errorMessage)
        {
            try
            {
                using var connection = MauiApp2.Components.Database.db.GetConnection();
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    INSERT INTO tbl_connectivity_log (is_online, response_time_ms, error_message)
                    VALUES (@is_online, @response_time_ms, @error_message)", connection);

                cmd.Parameters.AddWithValue("@is_online", isOnline);
                cmd.Parameters.AddWithValue("@response_time_ms", responseTime.HasValue ? (object)(int)responseTime.Value.TotalMilliseconds : DBNull.Value);
                cmd.Parameters.AddWithValue("@error_message", (object)errorMessage ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Silently fail - logging is not critical
            }
        }
    }
}



