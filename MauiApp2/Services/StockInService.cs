using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IStockInService
    {
        Task<int> ReceiveStockFromPurchaseOrderAsync(int poId, List<StockInItem> items, string? notes, int userId);
        Task<List<StockIn>> GetStockInHistoryAsync();
        Task<StockIn> GetStockInByIdAsync(int stockInId);
        Task<List<StockInItem>> GetStockInItemsAsync(int stockInId);
    }

    public class StockInService : IStockInService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public StockInService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // Receive stock from Purchase Order - simple, one method does everything
        public async Task<int> ReceiveStockFromPurchaseOrderAsync(int poId, List<StockInItem> items, string? notes, int userId)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Get Purchase Order details within transaction
                var getPOCommand = new SqlCommand(@"
                    SELECT supplier_id, po_number FROM tbl_purchase_order WHERE po_id = @po_id", connection, transaction);
                getPOCommand.Parameters.AddWithValue("@po_id", poId);
                
                string? poNumber = null;
                int supplierId = 0;
                using (var reader = await getPOCommand.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int supplierIdOrdinal = reader.GetOrdinal("supplier_id");
                        int poNumberOrdinal = reader.GetOrdinal("po_number");
                        
                        if (reader.IsDBNull(supplierIdOrdinal))
                        {
                            throw new Exception("Purchase order not found");
                        }
                        supplierId = reader.GetInt32(supplierIdOrdinal);
                        poNumber = reader.IsDBNull(poNumberOrdinal) ? null : reader.GetString(poNumberOrdinal);
                    }
                    else
                    {
                        throw new Exception("Purchase order not found");
                    }
                }

                // Step 2: Generate Stock In Number
                string stockInNumber = await GenerateStockInNumberAsync(connection, transaction);

                // Step 3: Create Stock In header
                var stockInId = await CreateStockInHeaderAsync(connection, transaction, poId, supplierId, stockInNumber, notes, userId);

                // Step 4: Check if rejected columns exist (once, outside the loop)
                bool hasRejectedColumns = await ColumnExistsAsync(connection, transaction, "tbl_stock_in_items", "quantity_rejected");
                
                // Step 5: Create Stock In items and update inventory
                foreach (var item in items)
                {
                    // Skip items where both received and rejected are 0 (nothing to record)
                    if (item.quantity_received == 0 && (!hasRejectedColumns || item.quantity_rejected == 0))
                    {
                        continue;
                    }

                    // Insert stock in item (even if quantity_received is 0, if there are rejected items)
                    await CreateStockInItemAsync(connection, transaction, stockInId, item);

                    // Update product inventory only if quantity_received > 0
                    if (item.quantity_received > 0)
                    {
                        await UpdateProductInventoryAsync(connection, transaction, item.product_id, item.quantity_received, item.unit_cost);
                    }
                }

                // Step 6: Calculate total cost for Accounts Payable
                decimal totalCost = 0;
                foreach (var item in items)
                {
                    totalCost += item.quantity_received * item.unit_cost;
                }

                // Step 7: Create Accounts Payable record
                await CreateAccountsPayableAsync(connection, transaction, poId, supplierId, totalCost);

                // Step 8: Create automatic accounting ledger entries
                await CreateStockInLedgerEntriesAsync(connection, transaction, stockInId, stockInNumber, totalCost, items, userId);

                // Step 9: Update PO status to "Received"
                var updatePOCommand = new SqlCommand(@"
                    UPDATE tbl_purchase_order 
                    SET status = @status, modified_date = @modified_date
                    WHERE po_id = @po_id", connection, transaction);
                updatePOCommand.Parameters.AddWithValue("@po_id", poId);
                updatePOCommand.Parameters.AddWithValue("@status", "Received");
                updatePOCommand.Parameters.AddWithValue("@modified_date", DateTime.Now);
                await updatePOCommand.ExecuteNonQueryAsync();

                // Commit transaction
                transaction.Commit();

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    string poNumberDisplay = poNumber ?? $"PO ID {poId}";
                    await _auditLogService.LogActionAsync(
                        userId,
                        "Create",
                        "tbl_stock_in",
                        stockInId,
                        null,
                        new { stock_in_number = stockInNumber, po_id = poId, supplier_id = supplierId, total_cost = totalCost, item_count = items.Count },
                        null,
                        null,
                        $"received stock from purchase order: {poNumberDisplay}"
                    );
                }

                return stockInId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error receiving stock: {ex.Message}");
            }
        }

        // Generate Stock In Number (SI-001, SI-002, etc.)
        private async Task<string> GenerateStockInNumberAsync(SqlConnection connection, SqlTransaction transaction)
        {
            var command = new SqlCommand(@"
                SELECT COUNT(*) FROM tbl_stock_in", connection, transaction);
            
            var count = (int)await command.ExecuteScalarAsync();
            return $"SI-{(count + 1).ToString("D3")}";
        }

        // Create Stock In header
        private async Task<int> CreateStockInHeaderAsync(SqlConnection connection, SqlTransaction transaction, int poId, int supplierId, string stockInNumber, string? notes, int userId)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_stock_in (po_id, supplier_id, stock_in_number, stock_in_date, notes, processed_by, created_date)
                VALUES (@po_id, @supplier_id, @stock_in_number, @stock_in_date, @notes, @processed_by, @created_date);
                SELECT SCOPE_IDENTITY();", connection, transaction);

            command.Parameters.AddWithValue("@po_id", poId);
            command.Parameters.AddWithValue("@supplier_id", supplierId);
            command.Parameters.AddWithValue("@stock_in_number", stockInNumber);
            command.Parameters.AddWithValue("@stock_in_date", DateTime.Now);
            command.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);
            command.Parameters.AddWithValue("@processed_by", userId);
            command.Parameters.AddWithValue("@created_date", DateTime.Now);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Create Stock In item
        private async Task CreateStockInItemAsync(SqlConnection connection, SqlTransaction transaction, int stockInId, StockInItem item)
        {
            // Build dynamic INSERT based on column existence
            var insertColumns = new List<string> { "stock_in_id", "product_id", "quantity_received", "unit_cost", "created_date" };
            var insertValues = new List<string> { "@stock_in_id", "@product_id", "@quantity_received", "@unit_cost", "@created_date" };
            
            // Check if rejected columns exist and add them if they do
            bool hasRejectedColumns = await ColumnExistsAsync(connection, transaction, "tbl_stock_in_items", "quantity_rejected");
            if (hasRejectedColumns)
            {
                insertColumns.Add("quantity_rejected");
                insertValues.Add("@quantity_rejected");
            }
            
            bool hasRejectionReason = await ColumnExistsAsync(connection, transaction, "tbl_stock_in_items", "rejection_reason");
            if (hasRejectionReason)
            {
                insertColumns.Add("rejection_reason");
                insertValues.Add("@rejection_reason");
            }
            
            bool hasRejectionRemarks = await ColumnExistsAsync(connection, transaction, "tbl_stock_in_items", "rejection_remarks");
            if (hasRejectionRemarks)
            {
                insertColumns.Add("rejection_remarks");
                insertValues.Add("@rejection_remarks");
            }

            var insertSql = $@"
                INSERT INTO tbl_stock_in_items ({string.Join(", ", insertColumns)})
                VALUES ({string.Join(", ", insertValues)})";

            var command = new SqlCommand(insertSql, connection, transaction);

            command.Parameters.AddWithValue("@stock_in_id", stockInId);
            command.Parameters.AddWithValue("@product_id", item.product_id);
            command.Parameters.AddWithValue("@quantity_received", item.quantity_received);
            command.Parameters.AddWithValue("@unit_cost", item.unit_cost);
            command.Parameters.AddWithValue("@created_date", DateTime.Now);
            
            if (hasRejectedColumns)
            {
                command.Parameters.AddWithValue("@quantity_rejected", item.quantity_rejected);
            }
            
            if (hasRejectionReason)
            {
                command.Parameters.AddWithValue("@rejection_reason", (object)item.rejection_reason ?? DBNull.Value);
            }
            
            if (hasRejectionRemarks)
            {
                command.Parameters.AddWithValue("@rejection_remarks", (object)item.rejection_remarks ?? DBNull.Value);
            }

            await command.ExecuteNonQueryAsync();
        }

        // Helper method to check if a column exists in a table
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
                
                var count = (int)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        // Update product inventory (increase quantity and update cost price)
        private async Task UpdateProductInventoryAsync(SqlConnection connection, SqlTransaction transaction, int productId, int quantityReceived, decimal unitCost)
        {
            // Update quantity (increase)
            var updateQuantityCommand = new SqlCommand(@"
                UPDATE tbl_product 
                SET quantity = ISNULL(quantity, 0) + @quantity_received,
                    cost_price = @unit_cost,
                    modified_date = @modified_date
                WHERE product_id = @product_id", connection, transaction);

            updateQuantityCommand.Parameters.AddWithValue("@product_id", productId);
            updateQuantityCommand.Parameters.AddWithValue("@quantity_received", quantityReceived);
            updateQuantityCommand.Parameters.AddWithValue("@unit_cost", unitCost);
            updateQuantityCommand.Parameters.AddWithValue("@modified_date", DateTime.Now);

            await updateQuantityCommand.ExecuteNonQueryAsync();
        }

        // Get Stock In history
        public async Task<List<StockIn>> GetStockInHistoryAsync()
        {
            var stockIns = new List<StockIn>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT si.stock_in_id, si.po_id, si.supplier_id, si.stock_in_number, si.stock_in_date, 
                           si.notes, si.processed_by, si.created_date,
                           s.supplier_name, po.po_number, u.full_name
                    FROM tbl_stock_in si
                    LEFT JOIN tbl_supplier s ON si.supplier_id = s.supplier_id
                    LEFT JOIN tbl_purchase_order po ON si.po_id = po.po_id
                    LEFT JOIN tbl_users u ON si.processed_by = u.user_id
                    ORDER BY si.stock_in_date DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    stockIns.Add(new StockIn
                    {
                        stock_in_id = reader.GetInt32(0),
                        po_id = reader.GetInt32(1),
                        supplier_id = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        stock_in_number = reader.GetString(3),
                        stock_in_date = reader.GetDateTime(4),
                        notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                        processed_by = reader.GetInt32(6),
                        created_date = reader.GetDateTime(7),
                        supplier_name = reader.IsDBNull(8) ? null : reader.GetString(8),
                        po_number = reader.IsDBNull(9) ? null : reader.GetString(9),
                        processed_by_name = reader.IsDBNull(10) ? null : reader.GetString(10)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_stock_in'"))
                {
                    Console.WriteLine("tbl_stock_in table doesn't exist yet.");
                    return stockIns;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading stock in history: {ex.Message}");
            }

            return stockIns;
        }

        // Get Stock In by ID
        public async Task<StockIn> GetStockInByIdAsync(int stockInId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT si.stock_in_id, si.po_id, si.supplier_id, si.stock_in_number, si.stock_in_date, 
                           si.notes, si.processed_by, si.created_date,
                           s.supplier_name, po.po_number, u.full_name
                    FROM tbl_stock_in si
                    LEFT JOIN tbl_supplier s ON si.supplier_id = s.supplier_id
                    LEFT JOIN tbl_purchase_order po ON si.po_id = po.po_id
                    LEFT JOIN tbl_users u ON si.processed_by = u.user_id
                    WHERE si.stock_in_id = @stock_in_id", connection);

                command.Parameters.AddWithValue("@stock_in_id", stockInId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new StockIn
                    {
                        stock_in_id = reader.GetInt32(0),
                        po_id = reader.GetInt32(1),
                        supplier_id = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        stock_in_number = reader.GetString(3),
                        stock_in_date = reader.GetDateTime(4),
                        notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                        processed_by = reader.GetInt32(6),
                        created_date = reader.GetDateTime(7),
                        supplier_name = reader.IsDBNull(8) ? null : reader.GetString(8),
                        po_number = reader.IsDBNull(9) ? null : reader.GetString(9),
                        processed_by_name = reader.IsDBNull(10) ? null : reader.GetString(10)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading stock in: {ex.Message}");
            }

            throw new Exception("Stock In not found");
        }

        // Get Stock In items
        public async Task<List<StockInItem>> GetStockInItemsAsync(int stockInId)
        {
            var items = new List<StockInItem>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if columns exist
                bool hasQuantityRejected = await ColumnExistsAsync(connection, null, "tbl_stock_in_items", "quantity_rejected");
                bool hasRejectionReason = await ColumnExistsAsync(connection, null, "tbl_stock_in_items", "rejection_reason");
                bool hasRejectionRemarks = await ColumnExistsAsync(connection, null, "tbl_stock_in_items", "rejection_remarks");

                string selectColumns = "sii.stock_in_items_id, sii.stock_in_id, sii.product_id, sii.quantity_received, sii.unit_cost, sii.created_date";
                if (hasQuantityRejected) selectColumns += ", sii.quantity_rejected";
                else selectColumns += ", 0 as quantity_rejected";
                if (hasRejectionReason) selectColumns += ", sii.rejection_reason";
                else selectColumns += ", NULL as rejection_reason";
                if (hasRejectionRemarks) selectColumns += ", sii.rejection_remarks";
                else selectColumns += ", NULL as rejection_remarks";
                selectColumns += ", p.product_name, p.product_sku";

                var command = new SqlCommand($@"
                    SELECT {selectColumns}
                    FROM tbl_stock_in_items sii
                    LEFT JOIN tbl_product p ON sii.product_id = p.product_id
                    WHERE sii.stock_in_id = @stock_in_id
                    ORDER BY sii.stock_in_items_id", connection);

                command.Parameters.AddWithValue("@stock_in_id", stockInId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var item = new StockInItem
                    {
                        stock_in_items_id = reader.GetInt32(reader.GetOrdinal("stock_in_items_id")),
                        stock_in_id = reader.GetInt32(reader.GetOrdinal("stock_in_id")),
                        product_id = reader.GetInt32(reader.GetOrdinal("product_id")),
                        quantity_received = reader.GetInt32(reader.GetOrdinal("quantity_received")),
                        unit_cost = reader.GetDecimal(reader.GetOrdinal("unit_cost")),
                        created_date = reader.GetDateTime(reader.GetOrdinal("created_date"))
                    };

                    if (hasQuantityRejected)
                    {
                        int qtyRejectedOrdinal = reader.GetOrdinal("quantity_rejected");
                        item.quantity_rejected = reader.IsDBNull(qtyRejectedOrdinal) ? 0 : reader.GetInt32(qtyRejectedOrdinal);
                    }
                    else
                    {
                        item.quantity_rejected = 0;
                    }

                    if (hasRejectionReason)
                    {
                        int reasonOrdinal = reader.GetOrdinal("rejection_reason");
                        item.rejection_reason = reader.IsDBNull(reasonOrdinal) ? null : reader.GetString(reasonOrdinal);
                    }

                    if (hasRejectionRemarks)
                    {
                        int remarksOrdinal = reader.GetOrdinal("rejection_remarks");
                        item.rejection_remarks = reader.IsDBNull(remarksOrdinal) ? null : reader.GetString(remarksOrdinal);
                    }

                    int productNameOrdinal = reader.GetOrdinal("product_name");
                    int productSkuOrdinal = reader.GetOrdinal("product_sku");
                    item.product_name = reader.IsDBNull(productNameOrdinal) ? null : reader.GetString(productNameOrdinal);
                    item.product_sku = reader.IsDBNull(productSkuOrdinal) ? null : reader.GetString(productSkuOrdinal);

                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading stock in items: {ex.Message}");
            }

            return items;
        }

        // Create Accounts Payable record
        private async Task CreateAccountsPayableAsync(SqlConnection connection, SqlTransaction transaction, int poId, int supplierId, decimal totalAmount)
        {
            try
            {
                // Check if AP already exists for this PO
                var checkCommand = new SqlCommand(@"
                    SELECT COUNT(*) FROM tbl_accounts_payable WHERE po_id = @po_id", connection, transaction);
                checkCommand.Parameters.AddWithValue("@po_id", poId);
                var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;

                if (!exists)
                {
                    var command = new SqlCommand(@"
                        INSERT INTO tbl_accounts_payable 
                        (po_id, supplier_id, total_amount, paid_amount, status, created_date)
                        VALUES 
                        (@po_id, @supplier_id, @total_amount, 0, 'Unpaid', GETDATE())", connection, transaction);

                    command.Parameters.AddWithValue("@po_id", poId);
                    command.Parameters.AddWithValue("@supplier_id", supplierId);
                    command.Parameters.AddWithValue("@total_amount", totalAmount);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stock in
                Console.WriteLine($"Error creating accounts payable: {ex.Message}");
            }
        }

        // Create automatic ledger entries for stock in
        private async Task CreateStockInLedgerEntriesAsync(SqlConnection connection, SqlTransaction transaction, int stockInId, string stockInNumber, decimal totalCost, List<StockInItem> items, int userId)
        {
            try
            {
                // Get account IDs
                int inventoryAccountId = await GetAccountIdByCodeAsync(connection, transaction, "1002"); // Inventory
                int apAccountId = await GetAccountIdByCodeAsync(connection, transaction, "2001"); // Accounts Payable

                // If accounts don't exist, skip accounting
                if (inventoryAccountId == 0 || apAccountId == 0)
                {
                    Console.WriteLine("Warning: Chart of Accounts not set up. Skipping automatic ledger entries.");
                    return;
                }

                // Create ledger entries for total cost
                // Inventory Debit (increase inventory value)
                await CreateLedgerEntryAsync(connection, transaction, inventoryAccountId, totalCost, 0,
                    $"Stock In {stockInNumber}", "Purchase", stockInId, userId);

                // Accounts Payable Credit (increase debt)
                await CreateLedgerEntryAsync(connection, transaction, apAccountId, 0, totalCost,
                    $"Stock In {stockInNumber}", "Purchase", stockInId, userId);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stock in
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
                return 0;
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

