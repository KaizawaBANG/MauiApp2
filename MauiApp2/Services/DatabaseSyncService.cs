using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MauiApp2.Services
{
    public interface IDatabaseSyncService
    {
        Task<SyncResult> SyncDatabaseAsync(string localConnectionString, string cloudConnectionString);
        Task<bool> TestConnectionAsync(string connectionString);
    }

    public class DatabaseSyncService : IDatabaseSyncService
    {
        // Tables in order (respecting foreign key dependencies)
        private static readonly List<string> Tables = new List<string>
        {
            "tbl_roles",
            "tbl_users",
            "tbl_category",
            "tbl_brand",
            "tbl_tax",
            "tbl_product",
            "tbl_supplier",
            "tbl_purchase_order",
            "tbl_purchase_order_items",
            "tbl_stock_in",
            "tbl_stock_in_items",
            "tbl_sales_order",
            "tbl_sales_order_items",
            "tbl_stock_out",
            "tbl_stock_out_items"
        };

        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Connection string is empty");
                }
                
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                // Test a simple query to ensure connection is fully working
                using var cmd = new SqlCommand("SELECT 1", connection);
                await cmd.ExecuteScalarAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                // Re-throw to get error message in the UI
                throw new Exception($"Connection failed: {ex.Message}", ex);
            }
        }

        public async Task<SyncResult> SyncDatabaseAsync(string localConnectionString, string cloudConnectionString)
        {
            var result = new SyncResult();
            result.StartTime = DateTime.Now;

            try
            {
                // Test connections
                result.Messages.Add("Testing local database connection...");
                if (!await TestConnectionAsync(localConnectionString))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Failed to connect to local database";
                    return result;
                }
                result.Messages.Add("✓ Local database connection successful");

                result.Messages.Add("Testing cloud database connection...");
                if (!await TestConnectionAsync(cloudConnectionString))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Failed to connect to cloud database";
                    return result;
                }
                result.Messages.Add("✓ Cloud database connection successful");

                result.Messages.Add("");
                result.Messages.Add("Starting data synchronization...");

                // Sync each table with retry logic
                foreach (var tableName in Tables)
                {
                    var tableResult = await SyncTableWithRetryAsync(localConnectionString, cloudConnectionString, tableName, maxRetries: 3);
                    result.TotalTablesProcessed++;
                    result.TotalRowsCopied += tableResult.RowsCopied;
                    
                    if (tableResult.IsSuccess)
                    {
                        result.Messages.Add($"✓ {tableName}: {tableResult.RowsCopied} rows copied");
                    }
                    else
                    {
                        result.Messages.Add($"✗ {tableName}: {tableResult.ErrorMessage}");
                        result.HasWarnings = true;
                    }
                }

                result.IsSuccess = true;
                result.Messages.Add("");
                result.Messages.Add($"=== Sync Complete ===");
                result.Messages.Add($"Total tables processed: {result.TotalTablesProcessed}");
                result.Messages.Add($"Total rows copied: {result.TotalRowsCopied}");
                
                // Reset identity seeds after sync to prevent ID jumps
                result.Messages.Add("");
                result.Messages.Add("Resetting identity seeds in cloud database...");
                await ResetIdentitySeedsAsync(cloudConnectionString);
                result.Messages.Add("✓ Identity seeds reset successfully");
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                result.Messages.Add($"ERROR: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }
        
        private async Task ResetIdentitySeedsAsync(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                foreach (var tableName in Tables)
                {
                    try
                    {
                        var pkColumn = await GetPrimaryKeyColumnAsync(connection, tableName);
                        if (!string.IsNullOrEmpty(pkColumn))
                        {
                            // Get max ID
                            var maxIdQuery = $"SELECT ISNULL(MAX([{pkColumn}]), 0) FROM [{tableName}]";
                            using var maxCmd = new SqlCommand(maxIdQuery, connection);
                            var maxId = Convert.ToInt32(await maxCmd.ExecuteScalarAsync());
                            
                            // Reset identity seed
                            var reseedQuery = $"DBCC CHECKIDENT ('[{tableName}]', RESEED, {maxId})";
                            using var reseedCmd = new SqlCommand(reseedQuery, connection);
                            await reseedCmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch
                    {
                        // Skip if table doesn't exist or has no identity column
                        continue;
                    }
                }
            }
            catch
            {
                // If reset fails, continue - it's not critical
            }
        }

        private async Task<TableSyncResult> SyncTableWithRetryAsync(string localConnectionString, string cloudConnectionString, string tableName, int maxRetries = 3)
        {
            TableSyncResult? lastResult = null;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    lastResult = await SyncTableAsync(localConnectionString, cloudConnectionString, tableName);
                    if (lastResult.IsSuccess)
                    {
                        return lastResult;
                    }
                    
                    // If it's a connection error, wait and retry
                    if (lastResult.ErrorMessage.Contains("connection") || 
                        lastResult.ErrorMessage.Contains("network") ||
                        lastResult.ErrorMessage.Contains("transport"))
                    {
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(2000 * attempt); // Exponential backoff: 2s, 4s, 6s
                            continue;
                        }
                    }
                    
                    // If it's not a connection error, don't retry
                    return lastResult;
                }
                catch (Exception ex)
                {
                    lastResult = new TableSyncResult
                    {
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                    
                    if (attempt < maxRetries && (ex.Message.Contains("connection") || ex.Message.Contains("network")))
                    {
                        await Task.Delay(2000 * attempt);
                        continue;
                    }
                    
                    return lastResult;
                }
            }
            
            return lastResult ?? new TableSyncResult { IsSuccess = false, ErrorMessage = "Max retries exceeded" };
        }

        private async Task<TableSyncResult> SyncTableAsync(string localConnectionString, string cloudConnectionString, string tableName)
        {
            var result = new TableSyncResult();
            SqlConnection? localConn = null;
            SqlConnection? cloudConn = null;

            try
            {
                // Create connections with longer timeout for cloud
                localConn = new SqlConnection(localConnectionString);
                var cloudConnBuilder = new SqlConnectionStringBuilder(cloudConnectionString)
                {
                    ConnectTimeout = 60, // 60 seconds connection timeout
                    CommandTimeout = 300 // 5 minutes command timeout
                };
                cloudConn = new SqlConnection(cloudConnBuilder.ConnectionString);

                await localConn.OpenAsync();
                await cloudConn.OpenAsync();

                // Check if table exists in local database
                var tableExistsQuery = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @tableName";

                using var checkCmd = new SqlCommand(tableExistsQuery, localConn);
                checkCmd.Parameters.AddWithValue("@tableName", tableName);
                var tableExists = (int)await checkCmd.ExecuteScalarAsync() > 0;

                if (!tableExists)
                {
                    result.ErrorMessage = "Table doesn't exist in local database";
                    return result;
                }

                // Check if table exists in cloud database
                using var checkCloudCmd = new SqlCommand(tableExistsQuery, cloudConn);
                checkCloudCmd.Parameters.AddWithValue("@tableName", tableName);
                var cloudTableExists = (int)await checkCloudCmd.ExecuteScalarAsync() > 0;

                if (!cloudTableExists)
                {
                    result.ErrorMessage = "Table doesn't exist in cloud database. Please create the table first.";
                    return result;
                }

                // Get all data from local database
                var selectQuery = $"SELECT * FROM [{tableName}]";
                using var localCmd = new SqlCommand(selectQuery, localConn);
                using var reader = await localCmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    result.RowsCopied = 0;
                    result.IsSuccess = true;
                    return result;
                }

                // Get column names from local database
                var localColumns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    localColumns.Add(reader.GetName(i));
                }

                // Get column names from cloud database
                var cloudColumns = await GetTableColumnsAsync(cloudConn, tableName);
                
                // Check if table has IDENTITY column and get its name
                var hasIdentity = await HasIdentityColumnAsync(cloudConn, tableName);
                string? identityColumnName = null;
                if (hasIdentity)
                {
                    identityColumnName = await GetIdentityColumnNameAsync(cloudConn, tableName);
                }
                
                // Filter to only include columns that exist in both databases
                var columns = localColumns.Where(c => cloudColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
                
                // If identity column exists and IDENTITY_INSERT will be ON, ensure it's in the columns list
                bool identityColumnInLocal = false;
                string? localIdentityColumnName = null;
                if (hasIdentity && !string.IsNullOrEmpty(identityColumnName))
                {
                    // Find matching identity column name (case-insensitive) in local columns
                    localIdentityColumnName = localColumns.FirstOrDefault(c => 
                        string.Equals(c, identityColumnName, StringComparison.OrdinalIgnoreCase));
                    identityColumnInLocal = !string.IsNullOrEmpty(localIdentityColumnName);
                    
                    // If identity column exists in both databases but not in our list, add it
                    if (identityColumnInLocal && 
                        cloudColumns.Contains(identityColumnName, StringComparer.OrdinalIgnoreCase) &&
                        !columns.Any(c => string.Equals(c, localIdentityColumnName, StringComparison.OrdinalIgnoreCase)))
                    {
                        columns.Insert(0, localIdentityColumnName); // Add at beginning to ensure it's included
                    }
                }
                
                // Check for missing columns and log warnings
                var missingColumns = localColumns.Where(c => !cloudColumns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToList();
                if (missingColumns.Any())
                {
                    result.ErrorMessage = $"Missing columns in cloud database: {string.Join(", ", missingColumns)}";
                    // Continue anyway with available columns
                }

                if (!columns.Any())
                {
                    result.ErrorMessage = "No matching columns found between local and cloud databases";
                    return result;
                }

                // Set IDENTITY_INSERT ON only if identity column exists in both databases
                // This must be done BEFORE any inserts
                if (hasIdentity && !string.IsNullOrEmpty(identityColumnName) && identityColumnInLocal && !string.IsNullOrEmpty(localIdentityColumnName))
                {
                    // Verify identity column is in our columns list
                    var hasIdentityInList = columns.Any(c => 
                        string.Equals(c, localIdentityColumnName, StringComparison.OrdinalIgnoreCase));
                    
                    if (!hasIdentityInList)
                    {
                        result.ErrorMessage = $"Identity column '{identityColumnName}' must be included when IDENTITY_INSERT is ON";
                        return result;
                    }
                    
                    var identityQuery = $"SET IDENTITY_INSERT [{tableName}] ON";
                    using var identityCmd = new SqlCommand(identityQuery, cloudConn);
                    await identityCmd.ExecuteNonQueryAsync();
                }
                else if (hasIdentity && !identityColumnInLocal)
                {
                    // Identity column exists in cloud but not in local - don't use IDENTITY_INSERT
                    // Let SQL Server auto-generate the identity values
                    hasIdentity = false;
                }

                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    // Build INSERT statement
                    var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
                    var valueList = string.Join(", ", columns.Select(c => $"@{c}"));

                    var insertQuery = $"INSERT INTO [{tableName}] ({columnList}) VALUES ({valueList})";

                    // Check if row already exists (by primary key)
                    var cloudPkColumn = await GetPrimaryKeyColumnAsync(cloudConn, tableName);
                    bool rowExists = false;
                    object? pkValue = null;
                    
                    if (!string.IsNullOrEmpty(cloudPkColumn) && columns.Contains(cloudPkColumn, StringComparer.OrdinalIgnoreCase))
                    {
                        // Find the matching local column name (case-insensitive)
                        var localPkColumn = localColumns.FirstOrDefault(c => 
                            string.Equals(c, cloudPkColumn, StringComparison.OrdinalIgnoreCase));
                        
                        if (!string.IsNullOrEmpty(localPkColumn))
                        {
                            pkValue = reader[localPkColumn];
                            var rowExistsQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{cloudPkColumn}] = @pkValue";
                            using var rowExistsCmd = new SqlCommand(rowExistsQuery, cloudConn);
                            rowExistsCmd.Parameters.AddWithValue("@pkValue", pkValue);
                            rowExists = (int)await rowExistsCmd.ExecuteScalarAsync() > 0;
                        }
                    }

                    if (rowExists && !string.IsNullOrEmpty(cloudPkColumn) && pkValue != null)
                    {
                        // Update existing row
                        var updateColumns = columns.Where(c => !string.Equals(c, cloudPkColumn, StringComparison.OrdinalIgnoreCase)).ToList();
                        if (updateColumns.Any())
                        {
                            var setClause = string.Join(", ", updateColumns.Select(c => $"[{c}] = @{c}"));
                            var updateQuery = $"UPDATE [{tableName}] SET {setClause} WHERE [{cloudPkColumn}] = @pkValue";
                            
                            using var updateCmd = new SqlCommand(updateQuery, cloudConn);
                            
                            // Add parameters for update
                            foreach (var column in updateColumns)
                            {
                                var value = reader[column];
                                if (value == DBNull.Value)
                                {
                                    updateCmd.Parameters.AddWithValue($"@{column}", DBNull.Value);
                                }
                                else
                                {
                                    updateCmd.Parameters.AddWithValue($"@{column}", value);
                                }
                            }
                            
                            updateCmd.Parameters.AddWithValue("@pkValue", pkValue);
                            await updateCmd.ExecuteNonQueryAsync();
                            rowCount++;
                        }
                        continue; // Skip insert after update
                    }

                    using var insertCmd = new SqlCommand(insertQuery, cloudConn);

                    // Add parameters
                    foreach (var column in columns)
                    {
                        var value = reader[column];
                        if (value == DBNull.Value)
                        {
                            insertCmd.Parameters.AddWithValue($"@{column}", DBNull.Value);
                        }
                        else
                        {
                            insertCmd.Parameters.AddWithValue($"@{column}", value);
                        }
                    }

                    await insertCmd.ExecuteNonQueryAsync();
                    rowCount++;
                }

                if (hasIdentity)
                {
                    var identityQuery = $"SET IDENTITY_INSERT [{tableName}] OFF";
                    using var identityCmd = new SqlCommand(identityQuery, cloudConn);
                    await identityCmd.ExecuteNonQueryAsync();
                }

                result.RowsCopied = rowCount;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                // Ensure connections are properly closed
                try
                {
                    if (localConn != null && localConn.State != System.Data.ConnectionState.Closed)
                    {
                        localConn.Close();
                        localConn.Dispose();
                    }
                }
                catch { }
                
                try
                {
                    if (cloudConn != null && cloudConn.State != System.Data.ConnectionState.Closed)
                    {
                        cloudConn.Close();
                        cloudConn.Dispose();
                    }
                }
                catch { }
            }

            return result;
        }

        private async Task<bool> HasIdentityColumnAsync(SqlConnection connection, string tableName)
        {
            try
            {
                var query = @"
                    SELECT COUNT(*) 
                    FROM sys.columns c
                    INNER JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE t.name = @tableName 
                    AND c.is_identity = 1";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                var count = (int)await cmd.ExecuteScalarAsync();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> GetIdentityColumnNameAsync(SqlConnection connection, string tableName)
        {
            try
            {
                var query = @"
                    SELECT c.name
                    FROM sys.columns c
                    INNER JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE t.name = @tableName 
                    AND c.is_identity = 1";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> GetPrimaryKeyColumnAsync(SqlConnection connection, string tableName)
        {
            try
            {
                var query = @"
                    SELECT c.name
                    FROM sys.key_constraints kc
                    INNER JOIN sys.index_columns ic ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
                    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    INNER JOIN sys.tables t ON kc.parent_object_id = t.object_id
                    WHERE t.name = @tableName
                    AND kc.type = 'PK'";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<List<string>> GetTableColumnsAsync(SqlConnection connection, string tableName)
        {
            var columns = new List<string>();
            try
            {
                var query = @"
                    SELECT c.name
                    FROM sys.columns c
                    INNER JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE t.name = @tableName
                    ORDER BY c.column_id";

                using var cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@tableName", tableName);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    columns.Add(reader.GetString(0));
                }
            }
            catch
            {
                // Return empty list if table doesn't exist
            }
            return columns;
        }
    }

    public class SyncResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Messages { get; set; } = new List<string>();
        public int TotalTablesProcessed { get; set; }
        public int TotalRowsCopied { get; set; }
        public bool HasWarnings { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class TableSyncResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int RowsCopied { get; set; }
    }
}
