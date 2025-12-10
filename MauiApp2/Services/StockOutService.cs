using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IStockOutService
    {
        Task<int> CreateStockOutFromSaleAsync(SqlConnection connection, SqlTransaction transaction, int salesOrderId, List<StockOutItem> items, int userId);
        Task<int> CreateStandaloneStockOutAsync(List<StockOutItem> items, string reason, string? notes, int userId);
        Task<List<StockOut>> GetStockOutHistoryAsync();
        Task<StockOut> GetStockOutByIdAsync(int stockOutId);
        Task<List<StockOutItem>> GetStockOutItemsAsync(int stockOutId);
    }

    public class StockOutService : IStockOutService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public StockOutService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // Create Stock Out from Sale (called within Sales Order transaction)
        public async Task<int> CreateStockOutFromSaleAsync(SqlConnection connection, SqlTransaction transaction, int salesOrderId, List<StockOutItem> items, int userId)
        {
            // Generate Stock Out Number (STO-001, STO-002, etc.)
            string stockOutNumber = await GenerateStockOutNumberAsync(connection, transaction);

            // Create Stock Out header
            var stockOutId = await CreateStockOutHeaderAsync(connection, transaction, salesOrderId, stockOutNumber, "Sale", userId);

            // Create Stock Out items
            foreach (var item in items)
            {
                await CreateStockOutItemAsync(connection, transaction, stockOutId, item);
            }

            return stockOutId;
        }

        // Create Standalone Stock Out (for damage, expiry, etc. - not tied to sales)
        public async Task<int> CreateStandaloneStockOutAsync(List<StockOutItem> items, string reason, string? notes, int userId)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Validate items
                if (items == null || items.Count == 0)
                {
                    throw new Exception("At least one item is required for stock out");
                }

                // Validate and check inventory for each item
                foreach (var item in items)
                {
                    if (item.quantity <= 0)
                    {
                        throw new Exception($"Quantity must be greater than 0 for product ID {item.product_id}");
                    }

                    // Check available inventory
                    var product = await GetProductByIdAsync(connection, transaction, item.product_id);
                    if (product == null)
                    {
                        throw new Exception($"Product with ID {item.product_id} not found");
                    }

                    var availableQuantity = product.quantity ?? 0;
                    if (availableQuantity < item.quantity)
                    {
                        throw new Exception($"Insufficient inventory for {product.product_name}. Available: {availableQuantity}, Requested: {item.quantity}");
                    }
                }

                // Generate Stock Out Number
                string stockOutNumber = await GenerateStockOutNumberAsync(connection, transaction);

                // Create Stock Out header (no sales_order_id)
                var stockOutId = await CreateStockOutHeaderAsync(connection, transaction, null, stockOutNumber, reason, userId);

                // Create Stock Out items and reduce inventory
                decimal totalCost = 0;
                foreach (var item in items)
                {
                    // Set reason on item if not set
                    if (string.IsNullOrWhiteSpace(item.reason))
                    {
                        item.reason = reason;
                    }

                    await CreateStockOutItemAsync(connection, transaction, stockOutId, item);

                    // Reduce inventory
                    await ReduceProductInventoryAsync(connection, transaction, item.product_id, item.quantity);

                    // Calculate total cost for ledger entries
                    var product = await GetProductByIdAsync(connection, transaction, item.product_id);
                    if (product != null && product.cost_price.HasValue)
                    {
                        totalCost += product.cost_price.Value * item.quantity;
                    }
                }

                // Create ledger entries for damaged/missing/disposal items (expense write-off)
                if ((reason.Equals("Damaged", StringComparison.OrdinalIgnoreCase) || 
                     reason.Equals("Missing", StringComparison.OrdinalIgnoreCase) ||
                     reason.Equals("Disposal", StringComparison.OrdinalIgnoreCase) ||
                     reason.Equals("damaged", StringComparison.OrdinalIgnoreCase) ||
                     reason.Equals("missing", StringComparison.OrdinalIgnoreCase) ||
                     reason.Equals("disposal", StringComparison.OrdinalIgnoreCase)) && totalCost > 0)
                {
                    await CreateExpenseLedgerEntriesAsync(connection, transaction, stockOutId, stockOutNumber, reason, totalCost, notes, userId);
                }

                // Commit transaction
                transaction.Commit();

                // Log audit action
                if (_auditLogService != null)
                {
                    try
                    {
                        var itemDescriptions = string.Join(", ", items.Select(item => $"{item.quantity} units"));
                        await _auditLogService.LogActionAsync(
                            userId,
                            "StockOut",
                            "tbl_stock_out",
                            stockOutId,
                            null,
                            new { reason, items, totalCost, notes },
                            null,
                            null,
                            $"Stock out created: {reason} - {itemDescriptions}" + (!string.IsNullOrWhiteSpace(notes) ? $" - {notes}" : "")
                        );
                    }
                    catch (Exception auditEx)
                    {
                        // Don't fail stock out if audit logging fails
                        Console.WriteLine($"Error logging audit action: {auditEx.Message}");
                    }
                }

                return stockOutId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error creating stock out: {ex.Message}", ex);
            }
        }

        // Generate Stock Out Number (STO-001, STO-002, etc.)
        private async Task<string> GenerateStockOutNumberAsync(SqlConnection connection, SqlTransaction transaction)
        {
            var command = new SqlCommand(@"
                SELECT COUNT(*) FROM tbl_stock_out", connection, transaction);
            
            var count = (int)await command.ExecuteScalarAsync();
            return $"STO-{(count + 1).ToString("D3")}";
        }

        // Create Stock Out header
        private async Task<int> CreateStockOutHeaderAsync(SqlConnection connection, SqlTransaction transaction, int? salesOrderId, string stockOutNumber, string reason, int userId)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_stock_out (sales_order_id, stock_out_number, stock_out_date, reason, processed_by, created_date)
                VALUES (@sales_order_id, @stock_out_number, @stock_out_date, @reason, @processed_by, @created_date);
                SELECT SCOPE_IDENTITY();", connection, transaction);

            command.Parameters.AddWithValue("@sales_order_id", (object?)salesOrderId ?? DBNull.Value);
            command.Parameters.AddWithValue("@stock_out_number", stockOutNumber);
            command.Parameters.AddWithValue("@stock_out_date", DateTime.Now);
            command.Parameters.AddWithValue("@reason", reason);
            command.Parameters.AddWithValue("@processed_by", userId);
            command.Parameters.AddWithValue("@created_date", DateTime.Now);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Create Stock Out item
        private async Task CreateStockOutItemAsync(SqlConnection connection, SqlTransaction transaction, int stockOutId, StockOutItem item)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_stock_out_items (stock_out_id, product_id, quantity, reason, created_date)
                VALUES (@stock_out_id, @product_id, @quantity, @reason, @created_date)", connection, transaction);

            command.Parameters.AddWithValue("@stock_out_id", stockOutId);
            command.Parameters.AddWithValue("@product_id", item.product_id);
            command.Parameters.AddWithValue("@quantity", item.quantity);
            command.Parameters.AddWithValue("@reason", item.reason ?? "Sale");
            command.Parameters.AddWithValue("@created_date", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }

        // Get Stock Out history
        public async Task<List<StockOut>> GetStockOutHistoryAsync()
        {
            var stockOuts = new List<StockOut>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT so.stock_out_id, so.sales_order_id, so.stock_out_number, so.stock_out_date, 
                           so.reason, so.processed_by, so.created_date,
                           u.full_name,
                           sales.sales_order_number
                    FROM tbl_stock_out so
                    LEFT JOIN tbl_users u ON so.processed_by = u.user_id
                    LEFT JOIN tbl_sales_order sales ON so.sales_order_id = sales.sales_order_id
                    ORDER BY so.stock_out_date DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    stockOuts.Add(new StockOut
                    {
                        stock_out_id = reader.GetInt32(0),
                        sales_order_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        stock_out_number = reader.GetString(2),
                        stock_out_date = reader.GetDateTime(3),
                        reason = reader.GetString(4),
                        processed_by = reader.GetInt32(5),
                        created_date = reader.GetDateTime(6),
                        processed_by_name = reader.IsDBNull(7) ? null : reader.GetString(7),
                        sales_order_number = reader.IsDBNull(8) ? null : reader.GetString(8)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading stock out history: {ex.Message}");
            }

            return stockOuts;
        }

        // Get Stock Out by ID
        public async Task<StockOut> GetStockOutByIdAsync(int stockOutId)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(@"
                SELECT so.stock_out_id, so.sales_order_id, so.stock_out_number, so.stock_out_date, 
                       so.reason, so.processed_by, so.created_date,
                       u.full_name,
                       sales.sales_order_number
                FROM tbl_stock_out so
                LEFT JOIN tbl_users u ON so.processed_by = u.user_id
                LEFT JOIN tbl_sales_order sales ON so.sales_order_id = sales.sales_order_id
                WHERE so.stock_out_id = @stock_out_id", connection);

            command.Parameters.AddWithValue("@stock_out_id", stockOutId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new StockOut
                {
                    stock_out_id = reader.GetInt32(0),
                    sales_order_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    stock_out_number = reader.GetString(2),
                    stock_out_date = reader.GetDateTime(3),
                    reason = reader.GetString(4),
                    processed_by = reader.GetInt32(5),
                    created_date = reader.GetDateTime(6),
                    processed_by_name = reader.IsDBNull(7) ? null : reader.GetString(7),
                    sales_order_number = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
            }

            throw new Exception("Stock out record not found");
        }

        // Get Stock Out items
        public async Task<List<StockOutItem>> GetStockOutItemsAsync(int stockOutId)
        {
            var items = new List<StockOutItem>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT soi.stock_out_items_id, soi.stock_out_id, soi.product_id, soi.quantity, 
                           soi.reason, soi.created_date,
                           p.product_name, p.product_sku, p.cost_price
                    FROM tbl_stock_out_items soi
                    LEFT JOIN tbl_product p ON soi.product_id = p.product_id
                    WHERE soi.stock_out_id = @stock_out_id
                    ORDER BY soi.stock_out_items_id", connection);

                command.Parameters.AddWithValue("@stock_out_id", stockOutId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new StockOutItem
                    {
                        stock_out_items_id = reader.GetInt32(0),
                        stock_out_id = reader.GetInt32(1),
                        product_id = reader.GetInt32(2),
                        quantity = reader.GetInt32(3),
                        reason = reader.GetString(4),
                        created_date = reader.GetDateTime(5),
                        product_name = reader.IsDBNull(6) ? null : reader.GetString(6),
                        product_sku = reader.IsDBNull(7) ? null : reader.GetString(7),
                        cost_price = reader.IsDBNull(8) ? null : reader.GetDecimal(8)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading stock out items: {ex.Message}");
            }

            return items;
        }

        // Reduce product inventory (quantity decreases)
        private async Task ReduceProductInventoryAsync(SqlConnection connection, SqlTransaction transaction, int productId, int quantity)
        {
            var command = new SqlCommand(@"
                UPDATE tbl_product 
                SET quantity = ISNULL(quantity, 0) - @quantity,
                    modified_date = @modified_date
                WHERE product_id = @product_id", connection, transaction);

            command.Parameters.AddWithValue("@product_id", productId);
            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@modified_date", DateTime.Now);

            await command.ExecuteNonQueryAsync();
        }

        // Get product by ID
        private async Task<Product?> GetProductByIdAsync(SqlConnection connection, SqlTransaction transaction, int productId)
        {
            var command = new SqlCommand(@"
                SELECT product_id, brand_id, category_id, tax_id, product_name, product_sku, 
                       model_number, cost_price, sell_price, quantity, status, created_date, modified_date
                FROM tbl_product
                WHERE product_id = @product_id", connection, transaction);

            command.Parameters.AddWithValue("@product_id", productId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Product
                {
                    product_id = reader.GetInt32(0),
                    brand_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    category_id = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    tax_id = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    product_name = reader.GetString(4),
                    product_sku = reader.GetString(5),
                    model_number = reader.IsDBNull(6) ? null : reader.GetString(6),
                    cost_price = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    sell_price = reader.GetDecimal(8),
                    quantity = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    status = reader.IsDBNull(10) ? null : reader.GetBoolean(10),
                    created_date = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    modified_date = reader.IsDBNull(12) ? null : reader.GetDateTime(12)
                };
            }
            return null;
        }

        // Create expense ledger entries for damaged/expired items
        private async Task CreateExpenseLedgerEntriesAsync(SqlConnection connection, SqlTransaction transaction, int stockOutId, string stockOutNumber, string reason, decimal totalCost, string? notes, int userId)
        {
            try
            {
                // Get account IDs
                int inventoryAccountId = await GetAccountIdByCodeAsync(connection, transaction, "1002"); // Inventory
                int expenseAccountId = await GetAccountIdByCodeAsync(connection, transaction, "5008"); // Inventory Loss/Damage

                // If accounts don't exist, skip accounting (graceful degradation)
                if (inventoryAccountId == 0 || expenseAccountId == 0)
                {
                    Console.WriteLine("Warning: Chart of Accounts not set up. Skipping automatic ledger entries for stock out.");
                    return;
                }

                string description = $"{reason} Stock Out {stockOutNumber}";
                if (!string.IsNullOrWhiteSpace(notes))
                {
                    description += $" - {notes}";
                }

                // Expense Debit (increase expense/loss)
                await CreateLedgerEntryAsync(connection, transaction, expenseAccountId, totalCost, 0,
                    description, "StockOut", stockOutId, userId);

                // Inventory Credit (reduce inventory asset)
                await CreateLedgerEntryAsync(connection, transaction, inventoryAccountId, 0, totalCost,
                    description, "StockOut", stockOutId, userId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stock out
                Console.WriteLine($"Error creating ledger entries: {ex.Message}");
            }
        }

        // Helper: Get account ID by code
        private async Task<int> GetAccountIdByCodeAsync(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            try
            {
                var command = new SqlCommand(@"
                    SELECT account_id FROM tbl_chart_of_accounts 
                    WHERE account_code = @account_code AND is_active = 1", connection, transaction);
                command.Parameters.AddWithValue("@account_code", accountCode);

                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0; // Account doesn't exist
            }
        }

        // Helper: Create ledger entry within transaction
        private async Task CreateLedgerEntryAsync(SqlConnection connection, SqlTransaction transaction, int accountId,
            decimal debitAmount, decimal creditAmount, string description, string referenceType, int referenceId, int createdBy)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_general_ledger 
                (transaction_date, account_id, debit_amount, credit_amount, description, 
                 reference_type, reference_id, created_by, created_date)
                VALUES 
                (GETDATE(), @account_id, @debit_amount, @credit_amount, @description,
                 @reference_type, @reference_id, @created_by, GETDATE())", connection, transaction);

            command.Parameters.AddWithValue("@account_id", accountId);
            command.Parameters.AddWithValue("@debit_amount", debitAmount);
            command.Parameters.AddWithValue("@credit_amount", creditAmount);
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@reference_type", referenceType);
            command.Parameters.AddWithValue("@reference_id", referenceId);
            command.Parameters.AddWithValue("@created_by", createdBy);

            await command.ExecuteNonQueryAsync();
        }
    }
}





