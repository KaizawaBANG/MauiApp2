using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(int userId, string actionType, string? tableName = null, int? recordId = null, 
            object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null, string? description = null);
        Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, 
            int? userId = null, string? actionType = null, string? tableName = null);
        Task<List<AuditLog>> GetAuditLogsByUserAsync(int userId, int limit = 100);
        Task<List<AuditLog>> GetAuditLogsByTableAsync(string tableName, int limit = 100);
    }

    public class AuditLogService : IAuditLogService
    {
        /// <summary>
        /// Logs an audit action to the database
        /// </summary>
        public async Task LogActionAsync(int userId, string actionType, string? tableName = null, 
            int? recordId = null, object? oldValues = null, object? newValues = null, 
            string? ipAddress = null, string? userAgent = null, string? description = null)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                string? oldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null;
                string? newValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null;

                // Check if description column exists
                bool hasDescriptionColumn = await ColumnExistsAsync(connection, null, "tbl_audit_log", "description");

                string insertColumns = "user_id, action_type, new_values, created_date";
                string insertValues = "@user_id, @action_type, @new_values, GETDATE()";
                
                if (hasDescriptionColumn)
                {
                    insertColumns += ", description";
                    insertValues += ", @description";
                }

                var command = new SqlCommand($@"
                    INSERT INTO tbl_audit_log 
                    ({insertColumns})
                    VALUES 
                    ({insertValues})", 
                    connection);

                command.Parameters.AddWithValue("@user_id", userId);
                command.Parameters.AddWithValue("@action_type", actionType);
                command.Parameters.AddWithValue("@new_values", (object?)newValuesJson ?? DBNull.Value);
                
                if (hasDescriptionColumn)
                {
                    command.Parameters.AddWithValue("@description", (object?)description ?? DBNull.Value);
                }

                await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                // Silently fail if table doesn't exist yet - allows app to work without audit table
                if (!ex.Message.Contains("Invalid object name 'tbl_audit_log'"))
                {
                    Console.WriteLine($"Error logging audit action: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // Don't throw - audit logging should not break the application
                Console.WriteLine($"Error logging audit action: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets audit logs with optional filters
        /// </summary>
        public async Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, 
            int? userId = null, string? actionType = null, string? tableName = null)
        {
            var logs = new List<AuditLog>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if description column exists
                bool hasDescriptionColumn = await ColumnExistsAsync(connection, null, "tbl_audit_log", "description");
                
                string descriptionSelect = hasDescriptionColumn ? ", al.description" : ", NULL as description";
                
                var query = $@"
                    SELECT al.log_id, al.user_id, al.action_type{descriptionSelect}, al.new_values, al.created_date,
                           u.username, u.full_name
                    FROM tbl_audit_log al
                    INNER JOIN tbl_users u ON al.user_id = u.user_id
                    WHERE 1=1";

                var command = new SqlCommand(query, connection);

                if (startDate.HasValue)
                {
                    query += " AND al.created_date >= @start_date";
                    command.Parameters.AddWithValue("@start_date", startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query += " AND al.created_date <= @end_date";
                    command.Parameters.AddWithValue("@end_date", endDate.Value);
                }

                if (userId.HasValue)
                {
                    query += " AND al.user_id = @user_id";
                    command.Parameters.AddWithValue("@user_id", userId.Value);
                }

                if (!string.IsNullOrWhiteSpace(actionType))
                {
                    query += " AND al.action_type = @action_type";
                    command.Parameters.AddWithValue("@action_type", actionType);
                }

                if (!string.IsNullOrWhiteSpace(tableName))
                {
                    query += " AND al.table_name = @table_name";
                    command.Parameters.AddWithValue("@table_name", tableName);
                }

                query += " ORDER BY al.created_date DESC";

                command.CommandText = query;

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int index = 0;
                    logs.Add(new AuditLog
                    {
                        log_id = reader.GetInt32(index++),
                        user_id = reader.GetInt32(index++),
                        action_type = reader.IsDBNull(index) ? string.Empty : reader.GetString(index++),
                        description = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        new_values = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        created_date = reader.GetDateTime(index++),
                        username = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        user_name = reader.IsDBNull(index) ? null : reader.GetString(index++)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_audit_log'"))
                {
                    Console.WriteLine("tbl_audit_log table doesn't exist yet.");
                    return logs;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading audit logs: {ex.Message}");
            }

            return logs;
        }

        /// <summary>
        /// Gets audit logs for a specific user
        /// </summary>
        public async Task<List<AuditLog>> GetAuditLogsByUserAsync(int userId, int limit = 100)
        {
            var logs = new List<AuditLog>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if description column exists
                bool hasDescriptionColumn = await ColumnExistsAsync(connection, null, "tbl_audit_log", "description");
                string descriptionSelect = hasDescriptionColumn ? ", al.description" : ", NULL as description";
                
                var command = new SqlCommand($@"
                    SELECT TOP (@limit) al.log_id, al.user_id, al.action_type{descriptionSelect}, al.new_values, al.created_date,
                           u.username, u.full_name
                    FROM tbl_audit_log al
                    INNER JOIN tbl_users u ON al.user_id = u.user_id
                    WHERE al.user_id = @user_id
                    ORDER BY al.created_date DESC", connection);

                command.Parameters.AddWithValue("@user_id", userId);
                command.Parameters.AddWithValue("@limit", limit);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int index = 0;
                    logs.Add(new AuditLog
                    {
                        log_id = reader.GetInt32(index++),
                        user_id = reader.GetInt32(index++),
                        action_type = reader.IsDBNull(index) ? string.Empty : reader.GetString(index++),
                        description = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        new_values = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        created_date = reader.GetDateTime(index++),
                        username = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        user_name = reader.IsDBNull(index) ? null : reader.GetString(index++)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_audit_log'"))
                {
                    Console.WriteLine("tbl_audit_log table doesn't exist yet.");
                    return logs;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading audit logs: {ex.Message}");
            }

            return logs;
        }

        /// <summary>
        /// Gets audit logs for a specific table
        /// </summary>
        public async Task<List<AuditLog>> GetAuditLogsByTableAsync(string tableName, int limit = 100)
        {
            var logs = new List<AuditLog>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if description column exists
                bool hasDescriptionColumn = await ColumnExistsAsync(connection, null, "tbl_audit_log", "description");
                string descriptionSelect = hasDescriptionColumn ? ", al.description" : ", NULL as description";
                
                var command = new SqlCommand($@"
                    SELECT TOP (@limit) al.log_id, al.user_id, al.action_type{descriptionSelect}, al.new_values, al.created_date,
                           u.username, u.full_name
                    FROM tbl_audit_log al
                    INNER JOIN tbl_users u ON al.user_id = u.user_id
                    ORDER BY al.created_date DESC", connection);
                command.Parameters.AddWithValue("@limit", limit);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int index = 0;
                    logs.Add(new AuditLog
                    {
                        log_id = reader.GetInt32(index++),
                        user_id = reader.GetInt32(index++),
                        action_type = reader.IsDBNull(index) ? string.Empty : reader.GetString(index++),
                        description = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        new_values = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        created_date = reader.GetDateTime(index++),
                        username = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        user_name = reader.IsDBNull(index) ? null : reader.GetString(index++)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_audit_log'"))
                {
                    Console.WriteLine("tbl_audit_log table doesn't exist yet.");
                    return logs;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading audit logs: {ex.Message}");
            }

            return logs;
        }

        /// <summary>
        /// Helper method to check if a column exists in a table
        /// </summary>
        private async Task<bool> ColumnExistsAsync(SqlConnection connection, SqlTransaction? transaction, string tableName, string columnName)
        {
            try
            {
                var command = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @table_name AND COLUMN_NAME = @column_name", connection, transaction);
                
                command.Parameters.AddWithValue("@table_name", tableName);
                command.Parameters.AddWithValue("@column_name", columnName);
                
                var result = await command.ExecuteScalarAsync();
                var count = result != null ? (int)result : 0;
                return count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
