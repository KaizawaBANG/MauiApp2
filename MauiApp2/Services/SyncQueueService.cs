using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public class SyncQueueService : ISyncQueueService
    {
        public async Task<int> AddToQueueAsync(string tableName, string operationType, int recordId, string? recordData = null)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    INSERT INTO tbl_sync_queue (table_name, operation_type, record_id, record_data, sync_status)
                    VALUES (@table_name, @operation_type, @record_id, @record_data, 'Pending');
                    SELECT SCOPE_IDENTITY();", connection);

                cmd.Parameters.AddWithValue("@table_name", tableName);
                cmd.Parameters.AddWithValue("@operation_type", operationType);
                cmd.Parameters.AddWithValue("@record_id", recordId);
                cmd.Parameters.AddWithValue("@record_data", (object)recordData ?? DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding to sync queue: {ex.Message}", ex);
            }
        }

        public async Task<List<SyncQueueItem>> GetPendingItemsAsync(int? limit = null)
        {
            var items = new List<SyncQueueItem>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT queue_id, table_name, operation_type, record_id, record_data, 
                           sync_status, error_message, retry_count, created_date, synced_date
                    FROM tbl_sync_queue
                    WHERE sync_status = 'Pending'
                    ORDER BY created_date DESC";

                if (limit.HasValue)
                {
                    query += $" OFFSET 0 ROWS FETCH NEXT {limit.Value} ROWS ONLY";
                }

                var cmd = new SqlCommand(query, connection);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new SyncQueueItem
                    {
                        QueueId = reader.GetInt32(0),
                        TableName = reader.GetString(1),
                        OperationType = reader.GetString(2),
                        RecordId = reader.GetInt32(3),
                        RecordData = reader.IsDBNull(4) ? null : reader.GetString(4),
                        SyncStatus = reader.GetString(5),
                        ErrorMessage = reader.IsDBNull(6) ? null : reader.GetString(6),
                        RetryCount = reader.GetInt32(7),
                        CreatedDate = reader.GetDateTime(8),
                        SyncedDate = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting pending items: {ex.Message}", ex);
            }

            return items;
        }

        public async Task<bool> MarkAsSyncingAsync(int queueId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    UPDATE tbl_sync_queue
                    SET sync_status = 'Syncing', last_attempt_date = GETDATE()
                    WHERE queue_id = @queue_id", connection);

                cmd.Parameters.AddWithValue("@queue_id", queueId);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MarkAsSyncedAsync(int queueId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    UPDATE tbl_sync_queue
                    SET sync_status = 'Synced', synced_date = GETDATE()
                    WHERE queue_id = @queue_id", connection);

                cmd.Parameters.AddWithValue("@queue_id", queueId);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> MarkAsFailedAsync(int queueId, string errorMessage)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var cmd = new SqlCommand(@"
                    UPDATE tbl_sync_queue
                    SET sync_status = 'Failed', 
                        error_message = @error_message,
                        retry_count = retry_count + 1,
                        last_attempt_date = GETDATE()
                    WHERE queue_id = @queue_id", connection);

                cmd.Parameters.AddWithValue("@queue_id", queueId);
                cmd.Parameters.AddWithValue("@error_message", errorMessage);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetPendingCountAsync()
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var cmd = new SqlCommand("SELECT COUNT(*) FROM tbl_sync_queue WHERE sync_status = 'Pending'", connection);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> ClearSyncedItemsAsync(DateTime? beforeDate = null)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = "DELETE FROM tbl_sync_queue WHERE sync_status = 'Synced'";
                if (beforeDate.HasValue)
                {
                    query += " AND synced_date < @before_date";
                }

                var cmd = new SqlCommand(query, connection);
                if (beforeDate.HasValue)
                {
                    cmd.Parameters.AddWithValue("@before_date", beforeDate.Value);
                }

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}



