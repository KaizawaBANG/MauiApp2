using System;
using System.Threading.Tasks;

namespace MauiApp2.Services
{
    public interface IConnectivityService
    {
        bool IsOnline { get; }
        DateTime LastCheckTime { get; }
        TimeSpan? LastResponseTime { get; }
        string LastError { get; }
        
        Task<bool> CheckCloudConnectivityAsync();
        Task<bool> IsCloudAvailableAsync();
        
        event EventHandler<bool> ConnectivityChanged;
        event EventHandler<ConnectivityStatus> StatusChanged;
    }

    public class ConnectivityStatus
    {
        public bool IsOnline { get; set; }
        public DateTime CheckTime { get; set; }
        public TimeSpan? ResponseTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}



