using System;
using System.Threading.Tasks;

namespace MauiApp2.Services
{
    public interface IAutoSyncService
    {
        bool IsSyncing { get; }
        bool IsEnabled { get; set; }
        DateTime? LastSyncTime { get; }
        SyncResult? LastSyncResult { get; }

        Task StartAsync();
        Task StopAsync();
        Task<bool> SyncNowAsync(bool force = false);
    }
}



