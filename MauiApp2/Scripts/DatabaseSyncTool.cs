using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace MauiApp2.Scripts
{
    /// <summary>
    /// Simple console app to sync data from local database to cloud database
    /// Usage: Update connection strings and run
    /// </summary>
    public class DatabaseSyncTool
    {
        // UPDATE THESE CONNECTION STRINGS
        private const string LOCAL_CONNECTION_STRING = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=DB_QuadTech;Integrated Security=True";
        private const string CLOUD_CONNECTION_STRING = "Data Source=db33496.public.databaseasp.net,1433;Initial Catalog=db33496;User ID=db33496;Password=4r%25M_6Wi3f%23P;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

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

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Database Sync Tool ===");
            Console.WriteLine("Syncing from LOCAL to CLOUD database...\n");

            try
            {
                // Test connections
                Console.WriteLine("Testing connections...");
                if (!await TestConnection(LOCAL_CONNECTION_STRING, "LOCAL"))
                {
                    Console.WriteLine("Failed to connect to LOCAL database. Exiting.");
                    return;
                }

                if (!await TestConnection(CLOUD_CONNECTION_STRING, "CLOUD"))
                {
                    Console.WriteLine("Failed to connect to CLOUD database. Exiting.");
                    return;
                }

                Console.WriteLine("Both connections successful!\n");

                // Sync each table
                foreach (var tableName in Tables)
                {
                    await SyncTable(tableName);
                }

                Console.WriteLine("\n=== Sync Complete! ===");
                Console.WriteLine("All data has been copied from LOCAL to CLOUD database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task<bool> TestConnection(string connectionString, string name)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                Console.WriteLine($"✓ {name} database connection successful");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ {name} database connection failed: {ex.Message}");
                return false;
            }
        }

        private static async Task SyncTable(string tableName)
        {
            try
            {
                Console.Write($"Syncing {tableName}... ");

                using var localConn = new SqlConnection(LOCAL_CONNECTION_STRING);
                using var cloudConn = new SqlConnection(CLOUD_CONNECTION_STRING);

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
                    Console.WriteLine("SKIPPED (table doesn't exist in local database)");
                    return;
                }

                // Get all data from local database
                var selectQuery = $"SELECT * FROM [{tableName}]";
                using var localCmd = new SqlCommand(selectQuery, localConn);
                using var reader = await localCmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    Console.WriteLine("SKIPPED (no data)");
                    return;
                }

                // Get column names
                var columns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columns.Add(reader.GetName(i));
                }

                // Check if table has IDENTITY column
                var hasIdentity = await HasIdentityColumn(cloudConn, tableName);
                if (hasIdentity)
                {
                    var identityQuery = $"SET IDENTITY_INSERT [{tableName}] ON";
                    using var identityCmd = new SqlCommand(identityQuery, cloudConn);
                    await identityCmd.ExecuteNonQueryAsync();
                }

                int rowCount = 0;
                while (await reader.ReadAsync())
                {
                    // Build INSERT statement
                    var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
                    var valueList = string.Join(", ", columns.Select(c => $"@{c}"));

                    var insertQuery = $"INSERT INTO [{tableName}] ({columnList}) VALUES ({valueList})";

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

                    // Check if row already exists (by primary key)
                    var pkColumn = await GetPrimaryKeyColumn(cloudConn, tableName);
                    if (!string.IsNullOrEmpty(pkColumn))
                    {
                        var pkValue = reader[pkColumn];
                        var rowExistsQuery = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{pkColumn}] = @pkValue";
                        using var rowExistsCmd = new SqlCommand(rowExistsQuery, cloudConn);
                        rowExistsCmd.Parameters.AddWithValue("@pkValue", pkValue);
                        var rowExists = (int)await rowExistsCmd.ExecuteScalarAsync() > 0;

                        if (rowExists)
                        {
                            continue; // Skip if already exists
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

                Console.WriteLine($"✓ ({rowCount} rows copied)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ERROR: {ex.Message}");
            }
        }

        private static async Task<bool> HasIdentityColumn(SqlConnection connection, string tableName)
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

        private static async Task<string> GetPrimaryKeyColumn(SqlConnection connection, string tableName)
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
    }
}

