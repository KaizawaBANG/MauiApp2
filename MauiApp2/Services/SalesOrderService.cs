using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface ISalesOrderService
    {
        Task<int> CreateSalesOrderAsync(DateTime salesDate, string paymentMethod, List<SalesOrderItem> items, int userId, int? customerId = null);
        Task<List<SalesOrder>> GetAllSalesOrdersAsync();
        Task<SalesOrder> GetSalesOrderByIdAsync(int salesOrderId);
        Task<List<SalesOrderItem>> GetSalesOrderItemsAsync(int salesOrderId);
    }

    public class SalesOrderService : ISalesOrderService
    {
        private readonly IStockOutService _stockOutService;
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public SalesOrderService(IStockOutService stockOutService, IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _stockOutService = stockOutService;
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // Create Sales Order and automatically create Stock Out
        public async Task<int> CreateSalesOrderAsync(DateTime salesDate, string paymentMethod, List<SalesOrderItem> items, int userId, int? customerId = null)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Generate Sales Order Number (INV-YYYYMM-NNNN, similar to PO format)
                string salesOrderNumber = await GenerateSalesOrderNumberAsync(connection, transaction, salesDate);

                // Step 2: Calculate totals and get tax rates for each item
                decimal subtotal = 0;
                decimal totalTax = 0;
                decimal totalAmount = 0;

                foreach (var item in items)
                {
                    // Get product details including tax rate
                    var product = await GetProductWithTaxAsync(connection, transaction, item.product_id);
                    if (product == null)
                    {
                        throw new Exception($"Product with ID {item.product_id} not found");
                    }

                    // Check stock availability
                    if (product.quantity < item.quantity)
                    {
                        throw new Exception($"Insufficient stock for product {product.product_name}. Available: {product.quantity}, Requested: {item.quantity}");
                    }

                    // Set unit price from product or use provided price
                    if (item.unit_price <= 0)
                    {
                        item.unit_price = product.sell_price;
                    }

                    // Get tax rate (default to 0 if no tax)
                    decimal taxRate = 0;
                    if (product.tax_id.HasValue)
                    {
                        taxRate = await GetTaxRateAsync(connection, transaction, product.tax_id.Value);
                    }

                    item.tax_rate = taxRate;

                    // Calculate item totals
                    item.subtotal = item.quantity * item.unit_price;
                    item.tax_amount = item.subtotal * taxRate;
                    item.total = item.subtotal + item.tax_amount;

                    subtotal += item.subtotal;
                    totalTax += item.tax_amount;
                    totalAmount += item.total;
                }

                // Step 3: Create Sales Order header
                var salesOrderId = await CreateSalesOrderHeaderAsync(connection, transaction, salesOrderNumber, salesDate, subtotal, totalTax, totalAmount, paymentMethod, userId, customerId);

                // Step 4: Create Sales Order items and reduce inventory
                foreach (var item in items)
                {
                    await CreateSalesOrderItemAsync(connection, transaction, salesOrderId, item);
                    
                    // Reduce product inventory (quantity decreases)
                    await ReduceProductInventoryAsync(connection, transaction, item.product_id, item.quantity);
                }

                // Step 5: Automatically create Stock Out (within same transaction)
                var stockOutItems = items.ConvertAll(item => new StockOutItem
                {
                    product_id = item.product_id,
                    quantity = item.quantity,
                    reason = "Sale"
                });

                await _stockOutService.CreateStockOutFromSaleAsync(connection, transaction, salesOrderId, stockOutItems, userId);

                // Step 6: Create automatic accounting ledger entries
                await CreateSalesLedgerEntriesAsync(connection, transaction, salesOrderId, salesOrderNumber, totalAmount, items, userId);

                // Commit transaction
                transaction.Commit();

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        userId,
                        "Create",
                        "tbl_sales_order",
                        salesOrderId,
                        null,
                        new { sales_order_number = salesOrderNumber, sales_date = salesDate, payment_method = paymentMethod, customer_id = customerId, total_amount = totalAmount, item_count = items.Count },
                        null,
                        null,
                        $"created sales order: {salesOrderNumber}"
                    );
                }

                return salesOrderId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error creating sales order: {ex.Message}");
            }
        }

        // Generate Sales Order Number (INV-YYYYMM-NNNN, similar to PO-YYYYMM-NNNN format)
        private async Task<string> GenerateSalesOrderNumberAsync(SqlConnection connection, SqlTransaction transaction, DateTime salesDate)
        {
            // Get year and month from sales date
            int year = salesDate.Year;
            int month = salesDate.Month;
            string yearMonth = $"{year}{month.ToString("D2")}"; // YYYYMM format
            
            // Find the highest invoice number for this year-month
            var command = new SqlCommand(@"
                SELECT ISNULL(MAX(CAST(SUBSTRING(sales_order_number, 10, 4) AS INT)), 0)
                FROM tbl_sales_order
                WHERE sales_order_number LIKE @pattern
                AND LEN(sales_order_number) = 13
                AND ISNUMERIC(SUBSTRING(sales_order_number, 10, 4)) = 1", connection, transaction);
            
            string pattern = $"INV-{yearMonth}-____";
            command.Parameters.AddWithValue("@pattern", pattern);
            
            var maxResult = await command.ExecuteScalarAsync();
            int nextNumber = 1;
            
            if (maxResult != null && !DBNull.Value.Equals(maxResult))
            {
                try
                {
                    nextNumber = Convert.ToInt32(maxResult) + 1;
                }
                catch
                {
                    nextNumber = 1; // Fallback if conversion fails
                }
            }
            
            // Generate invoice number and ensure uniqueness (handle race conditions)
            string invoiceNumber;
            int maxAttempts = 100; // Prevent infinite loop
            int attempts = 0;
            
            do
            {
                // Generate invoice number: INV-YYYYMM-NNNN
                invoiceNumber = $"INV-{yearMonth}-{nextNumber.ToString("D4")}";
                
                // Check for uniqueness within the transaction
                var checkCommand = new SqlCommand(
                    "SELECT COUNT(*) FROM tbl_sales_order WHERE sales_order_number = @sales_order_number",
                    connection, transaction);
                checkCommand.Parameters.AddWithValue("@sales_order_number", invoiceNumber);
                
                var count = (int)await checkCommand.ExecuteScalarAsync();
                
                if (count == 0)
                {
                    // Number is unique, break out of loop
                    break;
                }
                
                // Number exists, try next number
                nextNumber++;
                attempts++;
                
                // Safety check to prevent infinite loop
                if (attempts >= maxAttempts)
                {
                    throw new Exception($"Unable to generate unique sales order number after {maxAttempts} attempts. Please try again.");
                }
                
            } while (true);
            
            return invoiceNumber;
        }

        // Get product with tax info
        private async Task<Product> GetProductWithTaxAsync(SqlConnection connection, SqlTransaction transaction, int productId)
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

        // Get tax rate from tax_id
        private async Task<decimal> GetTaxRateAsync(SqlConnection connection, SqlTransaction transaction, int taxId)
        {
            var command = new SqlCommand(@"
                SELECT tax_rate FROM tbl_tax WHERE tax_id = @tax_id AND is_active = 1", connection, transaction);

            command.Parameters.AddWithValue("@tax_id", taxId);

            var result = await command.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDecimal(result);
            }
            return 0;
        }

        // Create Sales Order header
        private async Task<int> CreateSalesOrderHeaderAsync(SqlConnection connection, SqlTransaction transaction, string salesOrderNumber, DateTime salesDate, decimal subtotal, decimal taxAmount, decimal totalAmount, string paymentMethod, int userId, int? customerId = null)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_sales_order (sales_order_number, sales_date, subtotal, tax_amount, total_amount, payment_method, customer_id, processed_by, created_date)
                VALUES (@sales_order_number, @sales_date, @subtotal, @tax_amount, @total_amount, @payment_method, @customer_id, @processed_by, @created_date);
                SELECT SCOPE_IDENTITY();", connection, transaction);

            command.Parameters.AddWithValue("@sales_order_number", salesOrderNumber);
            command.Parameters.AddWithValue("@sales_date", salesDate);
            command.Parameters.AddWithValue("@subtotal", subtotal);
            command.Parameters.AddWithValue("@tax_amount", taxAmount);
            command.Parameters.AddWithValue("@total_amount", totalAmount);
            command.Parameters.AddWithValue("@payment_method", paymentMethod);
            command.Parameters.AddWithValue("@customer_id", customerId.HasValue ? (object)customerId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@processed_by", userId);
            command.Parameters.AddWithValue("@created_date", DateTime.Now);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // Create Sales Order item
        private async Task CreateSalesOrderItemAsync(SqlConnection connection, SqlTransaction transaction, int salesOrderId, SalesOrderItem item)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_sales_order_items (sales_order_id, product_id, quantity, unit_price, tax_rate, tax_amount, subtotal, total)
                VALUES (@sales_order_id, @product_id, @quantity, @unit_price, @tax_rate, @tax_amount, @subtotal, @total)", connection, transaction);

            command.Parameters.AddWithValue("@sales_order_id", salesOrderId);
            command.Parameters.AddWithValue("@product_id", item.product_id);
            command.Parameters.AddWithValue("@quantity", item.quantity);
            command.Parameters.AddWithValue("@unit_price", item.unit_price);
            command.Parameters.AddWithValue("@tax_rate", item.tax_rate);
            command.Parameters.AddWithValue("@tax_amount", item.tax_amount);
            command.Parameters.AddWithValue("@subtotal", item.subtotal);
            command.Parameters.AddWithValue("@total", item.total);

            await command.ExecuteNonQueryAsync();
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

        // Get all sales orders
        public async Task<List<SalesOrder>> GetAllSalesOrdersAsync()
        {
            var salesOrders = new List<SalesOrder>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT so.sales_order_id, so.sales_order_number, so.sales_date, so.subtotal, so.tax_amount, 
                           so.total_amount, so.payment_method, so.customer_id, so.processed_by, so.created_date,
                           u.full_name,
                           c.customer_name, c.contact_number, c.email, c.address,
                           (SELECT COUNT(*) FROM tbl_sales_order_items WHERE sales_order_id = so.sales_order_id) as item_count
                    FROM tbl_sales_order so
                    LEFT JOIN tbl_users u ON so.processed_by = u.user_id
                    LEFT JOIN tbl_customer c ON so.customer_id = c.customer_id
                    ORDER BY so.sales_date DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    salesOrders.Add(new SalesOrder
                    {
                        sales_order_id = reader.GetInt32(0),
                        sales_order_number = reader.GetString(1),
                        sales_date = reader.GetDateTime(2),
                        subtotal = reader.GetDecimal(3),
                        tax_amount = reader.GetDecimal(4),
                        total_amount = reader.GetDecimal(5),
                        payment_method = reader.GetString(6),
                        customer_id = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                        processed_by = reader.GetInt32(8),
                        created_date = reader.GetDateTime(9),
                        processed_by_name = reader.IsDBNull(10) ? null : reader.GetString(10),
                        customer_name = reader.IsDBNull(11) ? null : reader.GetString(11),
                        customer_contact = reader.IsDBNull(12) ? null : reader.GetString(12),
                        customer_email = reader.IsDBNull(13) ? null : reader.GetString(13),
                        customer_address = reader.IsDBNull(14) ? null : reader.GetString(14),
                        item_count = reader.IsDBNull(15) ? null : reader.GetInt32(15)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading sales orders: {ex.Message}");
            }

            return salesOrders;
        }

        // Get sales order by ID
        public async Task<SalesOrder> GetSalesOrderByIdAsync(int salesOrderId)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();

            var command = new SqlCommand(@"
                SELECT so.sales_order_id, so.sales_order_number, so.sales_date, so.subtotal, so.tax_amount, 
                       so.total_amount, so.payment_method, so.customer_id, so.processed_by, so.created_date,
                       u.full_name,
                       c.customer_name, c.contact_number, c.email, c.address,
                       (SELECT COUNT(*) FROM tbl_sales_order_items WHERE sales_order_id = so.sales_order_id) as item_count
                FROM tbl_sales_order so
                LEFT JOIN tbl_users u ON so.processed_by = u.user_id
                LEFT JOIN tbl_customer c ON so.customer_id = c.customer_id
                WHERE so.sales_order_id = @sales_order_id", connection);

            command.Parameters.AddWithValue("@sales_order_id", salesOrderId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new SalesOrder
                {
                    sales_order_id = reader.GetInt32(0),
                    sales_order_number = reader.GetString(1),
                    sales_date = reader.GetDateTime(2),
                    subtotal = reader.GetDecimal(3),
                    tax_amount = reader.GetDecimal(4),
                    total_amount = reader.GetDecimal(5),
                    payment_method = reader.GetString(6),
                    customer_id = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    processed_by = reader.GetInt32(8),
                    created_date = reader.GetDateTime(9),
                    processed_by_name = reader.IsDBNull(10) ? null : reader.GetString(10),
                    customer_name = reader.IsDBNull(11) ? null : reader.GetString(11),
                    customer_contact = reader.IsDBNull(12) ? null : reader.GetString(12),
                    customer_email = reader.IsDBNull(13) ? null : reader.GetString(13),
                    customer_address = reader.IsDBNull(14) ? null : reader.GetString(14),
                    item_count = reader.IsDBNull(15) ? null : reader.GetInt32(15)
                };
            }

            throw new Exception("Sales order not found");
        }

        // Get sales order items
        public async Task<List<SalesOrderItem>> GetSalesOrderItemsAsync(int salesOrderId)
        {
            var items = new List<SalesOrderItem>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT soi.sales_order_item_id, soi.sales_order_id, soi.product_id, soi.quantity, 
                           soi.unit_price, soi.tax_rate, soi.tax_amount, soi.subtotal, soi.total,
                           p.product_name, p.product_sku
                    FROM tbl_sales_order_items soi
                    LEFT JOIN tbl_product p ON soi.product_id = p.product_id
                    WHERE soi.sales_order_id = @sales_order_id
                    ORDER BY soi.sales_order_item_id", connection);

                command.Parameters.AddWithValue("@sales_order_id", salesOrderId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new SalesOrderItem
                    {
                        sales_order_item_id = reader.GetInt32(0),
                        sales_order_id = reader.GetInt32(1),
                        product_id = reader.GetInt32(2),
                        quantity = reader.GetInt32(3),
                        unit_price = reader.GetDecimal(4),
                        tax_rate = reader.GetDecimal(5),
                        tax_amount = reader.GetDecimal(6),
                        subtotal = reader.GetDecimal(7),
                        total = reader.GetDecimal(8),
                        product_name = reader.IsDBNull(9) ? null : reader.GetString(9),
                        product_sku = reader.IsDBNull(10) ? null : reader.GetString(10)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading sales order items: {ex.Message}");
            }

            return items;
        }

        // Create automatic ledger entries for sales
        private async Task CreateSalesLedgerEntriesAsync(SqlConnection connection, SqlTransaction transaction, int salesOrderId, string salesOrderNumber, decimal totalAmount, List<SalesOrderItem> items, int userId)
        {
            try
            {
                // Get account IDs (Cash, Sales Revenue, COGS, Inventory)
                int cashAccountId = await GetAccountIdByCodeAsync(connection, transaction, "1001"); // Cash
                int salesRevenueAccountId = await GetAccountIdByCodeAsync(connection, transaction, "4001"); // Sales Revenue
                int cogsAccountId = await GetAccountIdByCodeAsync(connection, transaction, "5001"); // COGS
                int inventoryAccountId = await GetAccountIdByCodeAsync(connection, transaction, "1002"); // Inventory

                // If accounts don't exist, skip accounting (graceful degradation)
                if (cashAccountId == 0 || salesRevenueAccountId == 0)
                {
                    Console.WriteLine("Warning: Chart of Accounts not set up. Skipping automatic ledger entries.");
                    return;
                }

                // 1. Record Cash (Debit) and Sales Revenue (Credit)
                await CreateLedgerEntryAsync(connection, transaction, cashAccountId, totalAmount, 0, 
                    $"Sale {salesOrderNumber}", "Sales", salesOrderId, userId);
                await CreateLedgerEntryAsync(connection, transaction, salesRevenueAccountId, 0, totalAmount, 
                    $"Sale {salesOrderNumber}", "Sales", salesOrderId, userId);

                // 2. Record COGS and Inventory for each item (if COGS and Inventory accounts exist)
                if (cogsAccountId > 0 && inventoryAccountId > 0)
                {
                    foreach (var item in items)
                    {
                        // Get product cost
                        var product = await GetProductWithTaxAsync(connection, transaction, item.product_id);
                        if (product != null && product.cost_price.HasValue)
                        {
                            decimal cogsAmount = product.cost_price.Value * item.quantity;
                            
                            // COGS Debit
                            await CreateLedgerEntryAsync(connection, transaction, cogsAccountId, cogsAmount, 0,
                                $"COGS for Sale {salesOrderNumber} - {product.product_name}", "Sales", salesOrderId, userId);
                            
                            // Inventory Credit
                            await CreateLedgerEntryAsync(connection, transaction, inventoryAccountId, 0, cogsAmount,
                                $"Inventory sold - Sale {salesOrderNumber} - {product.product_name}", "Sales", salesOrderId, userId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the sale
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




