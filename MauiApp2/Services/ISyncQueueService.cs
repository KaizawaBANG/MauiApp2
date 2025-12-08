using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MauiApp2.Services
{
    public interface ISyncQueueService
    {
        Task<int> AddToQueueAsync(string tableName, string operationType, int recordId, string? recordData = null);
        Task<List<SyncQueueItem>> GetPendingItemsAsync(int? limit = null);
        Task<bool> MarkAsSyncingAsync(int queueId);
        Task<bool> MarkAsSyncedAsync(int queueId);
        Task<bool> MarkAsFailedAsync(int queueId, string errorMessage);
        Task<int> GetPendingCountAsync();
        Task<bool> ClearSyncedItemsAsync(DateTime? beforeDate = null);
    }

    public class SyncQueueItem
    {
        public int QueueId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public int RecordId { get; set; }
        public string? RecordData { get; set; }
        public string SyncStatus { get; set; } = "Pending";
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? SyncedDate { get; set; }
    }
}



