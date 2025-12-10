using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IPurchaseOrderService
    {
        Task<List<PurchaseOrder>> GetPendingPurchaseOrdersAsync();
        Task<List<PurchaseOrder>> GetAllPurchaseOrdersAsync();
        Task<PurchaseOrder> GetPurchaseOrderByIdAsync(int poId);
        Task<List<PurchaseOrderItem>> GetPurchaseOrderItemsAsync(int poId);
        Task<bool> UpdatePurchaseOrderStatusAsync(int poId, string status, string? cancellationReason = null, string? cancellationRemarks = null);
        Task<int> CreatePurchaseOrderAsync(int supplierId, DateTime orderDate, DateTime expectedDate, string? notes, List<PurchaseOrderItem> items);
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public PurchaseOrderService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // Get pending purchase orders (for Stock In)
        public async Task<List<PurchaseOrder>> GetPendingPurchaseOrdersAsync()
        {
            var purchaseOrders = new List<PurchaseOrder>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if columns exist
                bool hasSubtotal = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "subtotal");
                bool hasTaxAmount = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "tax_amount");
                bool hasCancellationReason = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_reason");
                bool hasCancellationRemarks = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_remarks");
                
                string selectColumns = "po.po_id, po.supplier_id, po.po_number, po.order_date, po.expected_date, po.status, po.total_amount, po.notes";
                if (hasSubtotal) selectColumns += ", po.subtotal";
                else selectColumns += ", CAST(po.total_amount AS DECIMAL(12,2)) as subtotal";
                if (hasTaxAmount) selectColumns += ", po.tax_amount";
                else selectColumns += ", CAST(0 AS DECIMAL(12,2)) as tax_amount";
                if (hasCancellationReason) selectColumns += ", po.cancellation_reason";
                else selectColumns += ", NULL as cancellation_reason";
                if (hasCancellationRemarks) selectColumns += ", po.cancellation_remarks";
                else selectColumns += ", NULL as cancellation_remarks";
                selectColumns += ", po.created_date, po.modified_date, s.supplier_name";
                
                var command = new SqlCommand($@"
                    SELECT {selectColumns}
                    FROM tbl_purchase_order po
                    LEFT JOIN tbl_supplier s ON po.supplier_id = s.supplier_id
                    LEFT JOIN tbl_stock_in si ON po.po_id = si.po_id
                    WHERE po.status = 'Delivered' 
                      AND si.po_id IS NULL
                    ORDER BY po.order_date DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    purchaseOrders.Add(new PurchaseOrder
                    {
                        po_id = reader.GetInt32(reader.GetOrdinal("po_id")),
                        supplier_id = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                        po_number = reader.IsDBNull(reader.GetOrdinal("po_number")) ? "" : reader.GetString(reader.GetOrdinal("po_number")),
                        order_date = reader.IsDBNull(reader.GetOrdinal("order_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("order_date")),
                        expected_date = reader.IsDBNull(reader.GetOrdinal("expected_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("expected_date")),
                        status = reader.IsDBNull(reader.GetOrdinal("status")) ? "Pending" : reader.GetString(reader.GetOrdinal("status")),
                        total_amount = reader.IsDBNull(reader.GetOrdinal("total_amount")) ? 0 : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("total_amount"))),
                        notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                        subtotal = reader.IsDBNull(reader.GetOrdinal("subtotal")) ? 0 : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("subtotal"))),
                        tax_amount = reader.IsDBNull(reader.GetOrdinal("tax_amount")) ? 0 : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("tax_amount"))),
                        cancellation_reason = reader.IsDBNull(reader.GetOrdinal("cancellation_reason")) ? null : reader.GetString(reader.GetOrdinal("cancellation_reason")),
                        cancellation_remarks = reader.IsDBNull(reader.GetOrdinal("cancellation_remarks")) ? null : reader.GetString(reader.GetOrdinal("cancellation_remarks")),
                        created_date = reader.IsDBNull(reader.GetOrdinal("created_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("created_date")),
                        modified_date = reader.IsDBNull(reader.GetOrdinal("modified_date")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("modified_date")),
                        supplier_name = reader.IsDBNull(reader.GetOrdinal("supplier_name")) ? null : reader.GetString(reader.GetOrdinal("supplier_name"))
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_purchase_order'"))
                {
                    Console.WriteLine("tbl_purchase_order table doesn't exist yet.");
                    return purchaseOrders;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading purchase orders: {ex.Message}");
            }

            return purchaseOrders;
        }

        // Get purchase order by ID
        public async Task<PurchaseOrder> GetPurchaseOrderByIdAsync(int poId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if columns exist
                bool hasSubtotal = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "subtotal");
                bool hasTaxAmount = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "tax_amount");
                bool hasCancellationReason = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_reason");
                bool hasCancellationRemarks = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_remarks");
                
                string selectColumns = "po_id, supplier_id, po_number, order_date, expected_date, status, total_amount, notes";
                if (hasSubtotal) selectColumns += ", subtotal";
                else selectColumns += ", CAST(total_amount AS DECIMAL(12,2)) as subtotal";
                if (hasTaxAmount) selectColumns += ", tax_amount";
                else selectColumns += ", CAST(0 AS DECIMAL(12,2)) as tax_amount";
                if (hasCancellationReason) selectColumns += ", cancellation_reason";
                else selectColumns += ", NULL as cancellation_reason";
                if (hasCancellationRemarks) selectColumns += ", cancellation_remarks";
                else selectColumns += ", NULL as cancellation_remarks";
                selectColumns += ", created_date, modified_date";
                
                var command = new SqlCommand($@"
                    SELECT {selectColumns}
                    FROM tbl_purchase_order
                    WHERE po_id = @po_id", connection);

                command.Parameters.AddWithValue("@po_id", poId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int index = 0;
                    return new PurchaseOrder
                    {
                        po_id = reader.GetInt32(index++),
                        supplier_id = reader.GetInt32(index++),
                        po_number = reader.IsDBNull(index) ? "" : reader.GetString(index++),
                        order_date = reader.IsDBNull(index) ? DateTime.Now : reader.GetDateTime(index++),
                        expected_date = reader.IsDBNull(index) ? DateTime.Now : reader.GetDateTime(index++),
                        status = reader.IsDBNull(index) ? "Pending" : reader.GetString(index++),
                        total_amount = reader.IsDBNull(index) ? 0 : Convert.ToDecimal(reader.GetValue(index++)),
                        notes = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        subtotal = reader.IsDBNull(index) ? 0 : Convert.ToDecimal(reader.GetValue(index++)),
                        tax_amount = reader.IsDBNull(index) ? 0 : Convert.ToDecimal(reader.GetValue(index++)),
                        cancellation_reason = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        cancellation_remarks = reader.IsDBNull(index) ? null : reader.GetString(index++),
                        created_date = reader.IsDBNull(index) ? DateTime.Now : reader.GetDateTime(index++),
                        modified_date = reader.IsDBNull(index) ? null : (DateTime?)reader.GetDateTime(index++)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading purchase order: {ex.Message}");
            }

            throw new Exception("Purchase order not found");
        }

        // Get purchase order items with product details
        public async Task<List<PurchaseOrderItem>> GetPurchaseOrderItemsAsync(int poId)
        {
            var items = new List<PurchaseOrderItem>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check which primary key column name exists (po_item_id or po_items_id)
                bool hasPoItemId = await ColumnExistsAsync(connection, (SqlTransaction?)null, "tbl_purchase_order_items", "po_item_id");
                bool hasPoItemsId = await ColumnExistsAsync(connection, (SqlTransaction?)null, "tbl_purchase_order_items", "po_items_id");
                // Use po_item_id if it exists, otherwise default to po_items_id
                string pkColumnName = hasPoItemId ? "po_item_id" : (hasPoItemsId ? "po_items_id" : "po_item_id");
                
                // Check if tax columns exist
                bool hasCreatedDate = await ColumnExistsAsync(connection, (SqlTransaction?)null, "tbl_purchase_order_items", "created_date");
                bool hasTaxRate = await ColumnExistsAsync(connection, (SqlTransaction?)null, "tbl_purchase_order_items", "tax_rate");
                bool hasTaxAmount = await ColumnExistsAsync(connection, (SqlTransaction?)null, "tbl_purchase_order_items", "tax_amount");
                bool hasSubtotal = await ColumnExistsAsync(connection, (SqlTransaction?)null, "tbl_purchase_order_items", "subtotal");
                bool hasTotal = await ColumnExistsAsync(connection, (SqlTransaction?)null, "tbl_purchase_order_items", "total");
                
                var selectColumns = new List<string> { $"poi.{pkColumnName}", "poi.po_id", "poi.product_id", "poi.quantity_ordered", "poi.unit_cost" };
                if (hasTaxRate) selectColumns.Add("poi.tax_rate");
                if (hasTaxAmount) selectColumns.Add("poi.tax_amount");
                if (hasSubtotal) selectColumns.Add("poi.subtotal");
                if (hasTotal) selectColumns.Add("poi.total");
                if (hasCreatedDate) selectColumns.Add("poi.created_date");
                selectColumns.Add("p.product_name");
                selectColumns.Add("p.product_sku");

                var command = new SqlCommand($@"
                    SELECT {string.Join(", ", selectColumns)}
                    FROM tbl_purchase_order_items poi
                    INNER JOIN tbl_product p ON poi.product_id = p.product_id
                    WHERE poi.po_id = @po_id
                    ORDER BY poi.{pkColumnName}", connection);

                command.Parameters.AddWithValue("@po_id", poId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int index = 0;
                    var item = new PurchaseOrderItem
                    {
                        po_items_id = reader.GetInt32(index++),
                        po_id = reader.GetInt32(index++),
                        product_id = reader.GetInt32(index++),
                        quantity_ordered = reader.GetInt32(index++),
                        unit_cost = reader.GetDecimal(index++)
                    };

                    // Read tax fields if they exist (handle both INT and DECIMAL types)
                    if (hasTaxRate)
                    {
                        if (reader.IsDBNull(index))
                            item.tax_rate = 0;
                        else
                            item.tax_rate = Convert.ToDecimal(reader.GetValue(index));
                        index++;
                    }
                    if (hasTaxAmount)
                    {
                        if (reader.IsDBNull(index))
                            item.tax_amount = 0;
                        else
                            item.tax_amount = Convert.ToDecimal(reader.GetValue(index));
                        index++;
                    }
                    if (hasSubtotal)
                    {
                        if (reader.IsDBNull(index))
                            item.subtotal = 0;
                        else
                            item.subtotal = Convert.ToDecimal(reader.GetValue(index));
                        index++;
                    }
                    if (hasTotal)
                    {
                        if (reader.IsDBNull(index))
                            item.total = 0;
                        else
                            item.total = Convert.ToDecimal(reader.GetValue(index));
                        index++;
                    }
                    if (hasCreatedDate)
                    {
                        item.created_date = reader.GetDateTime(index++);
                    }
                    else
                    {
                        item.created_date = DateTime.Now; // Default value if column doesn't exist
                    }

                    item.product_name = reader.IsDBNull(index) ? null : reader.GetString(index++);
                    item.product_sku = reader.IsDBNull(index) ? null : reader.GetString(index++);

                    items.Add(item);
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_purchase_order_items'"))
                {
                    Console.WriteLine("tbl_purchase_order_items table doesn't exist yet.");
                    return items;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading purchase order items: {ex.Message}");
            }

            return items;
        }

        // Update purchase order status
        public async Task<bool> UpdatePurchaseOrderStatusAsync(int poId, string status, string? cancellationReason = null, string? cancellationRemarks = null)
        {
            try
            {
                // Get old values for audit log
                PurchaseOrder? oldPO = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    oldPO = await GetPurchaseOrderByIdAsync(poId);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if cancellation columns exist
                bool hasCancellationReason = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_reason");
                bool hasCancellationRemarks = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_remarks");
                
                string updateSql = @"
                    UPDATE tbl_purchase_order 
                    SET status = @status, modified_date = @modified_date";
                
                if (hasCancellationReason)
                {
                    updateSql += ", cancellation_reason = @cancellation_reason";
                }
                
                if (hasCancellationRemarks)
                {
                    updateSql += ", cancellation_remarks = @cancellation_remarks";
                }
                
                updateSql += " WHERE po_id = @po_id";

                var command = new SqlCommand(updateSql, connection);

                command.Parameters.AddWithValue("@po_id", poId);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@modified_date", DateTime.Now);
                
                if (hasCancellationReason)
                {
                    command.Parameters.AddWithValue("@cancellation_reason", (object)cancellationReason ?? DBNull.Value);
                }
                
                if (hasCancellationRemarks)
                {
                    command.Parameters.AddWithValue("@cancellation_remarks", (object)cancellationRemarks ?? DBNull.Value);
                }

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldPO != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_purchase_order",
                        poId,
                        new { status = oldPO.status },
                        new { status = status, cancellation_reason = cancellationReason, cancellation_remarks = cancellationRemarks },
                        null,
                        null,
                        $"updated purchase order status: {oldPO.po_number} to {status}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating purchase order status: {ex.Message}");
            }
        }

        // Get all purchase orders (for display in Purchase Order page)
        public async Task<List<PurchaseOrder>> GetAllPurchaseOrdersAsync()
        {
            var purchaseOrders = new List<PurchaseOrder>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Check if columns exist
                bool hasSubtotal = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "subtotal");
                bool hasTaxAmount = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "tax_amount");
                bool hasCancellationReason = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_reason");
                bool hasCancellationRemarks = await ColumnExistsAsync(connection, null, "tbl_purchase_order", "cancellation_remarks");
                
                string selectColumns = "po.po_id, po.supplier_id, po.po_number, po.order_date, po.expected_date, po.status, po.total_amount, po.notes";
                if (hasSubtotal) selectColumns += ", po.subtotal";
                else selectColumns += ", CAST(po.total_amount AS DECIMAL(12,2)) as subtotal";
                if (hasTaxAmount) selectColumns += ", po.tax_amount";
                else selectColumns += ", CAST(0 AS DECIMAL(12,2)) as tax_amount";
                if (hasCancellationReason) selectColumns += ", po.cancellation_reason";
                else selectColumns += ", NULL as cancellation_reason";
                if (hasCancellationRemarks) selectColumns += ", po.cancellation_remarks";
                else selectColumns += ", NULL as cancellation_remarks";
                selectColumns += ", po.created_date, po.modified_date, s.supplier_name";
                
                var command = new SqlCommand($@"
                    SELECT {selectColumns}
                    FROM tbl_purchase_order po
                    LEFT JOIN tbl_supplier s ON po.supplier_id = s.supplier_id
                    ORDER BY po.order_date DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    purchaseOrders.Add(new PurchaseOrder
                    {
                        po_id = reader.GetInt32(reader.GetOrdinal("po_id")),
                        supplier_id = reader.GetInt32(reader.GetOrdinal("supplier_id")),
                        po_number = reader.IsDBNull(reader.GetOrdinal("po_number")) ? "" : reader.GetString(reader.GetOrdinal("po_number")),
                        order_date = reader.IsDBNull(reader.GetOrdinal("order_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("order_date")),
                        expected_date = reader.IsDBNull(reader.GetOrdinal("expected_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("expected_date")),
                        status = reader.IsDBNull(reader.GetOrdinal("status")) ? "Pending" : reader.GetString(reader.GetOrdinal("status")),
                        total_amount = reader.IsDBNull(reader.GetOrdinal("total_amount")) ? 0 : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("total_amount"))),
                        notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes")),
                        subtotal = reader.IsDBNull(reader.GetOrdinal("subtotal")) ? 0 : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("subtotal"))),
                        tax_amount = reader.IsDBNull(reader.GetOrdinal("tax_amount")) ? 0 : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("tax_amount"))),
                        cancellation_reason = reader.IsDBNull(reader.GetOrdinal("cancellation_reason")) ? null : reader.GetString(reader.GetOrdinal("cancellation_reason")),
                        cancellation_remarks = reader.IsDBNull(reader.GetOrdinal("cancellation_remarks")) ? null : reader.GetString(reader.GetOrdinal("cancellation_remarks")),
                        created_date = reader.IsDBNull(reader.GetOrdinal("created_date")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("created_date")),
                        modified_date = reader.IsDBNull(reader.GetOrdinal("modified_date")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("modified_date")),
                        supplier_name = reader.IsDBNull(reader.GetOrdinal("supplier_name")) ? null : reader.GetString(reader.GetOrdinal("supplier_name"))
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_purchase_order'"))
                {
                    Console.WriteLine("tbl_purchase_order table doesn't exist yet.");
                    return purchaseOrders;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading purchase orders: {ex.Message}");
            }

            return purchaseOrders;
        }

        // Create purchase order with items
        public async Task<int> CreatePurchaseOrderAsync(int supplierId, DateTime orderDate, DateTime expectedDate, string? notes, List<PurchaseOrderItem> items)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Generate PO Number (based on order date)
                string poNumber = await GeneratePONumberAsync(connection, transaction, orderDate);

                // Calculate tax per item (from product's tax_id) and totals
                decimal orderSubtotal = 0;
                decimal orderTaxAmount = 0;
                decimal orderTotal = 0;

                foreach (var item in items)
                {
                    // Get product with tax info
                    var product = await GetProductWithTaxAsync(connection, transaction, item.product_id);
                    if (product == null)
                    {
                        throw new Exception($"Product with ID {item.product_id} not found");
                    }

                    // Get tax rate from product's tax_id
                    decimal taxRate = 0;
                    if (product.tax_id.HasValue)
                    {
                        taxRate = await GetTaxRateAsync(connection, transaction, product.tax_id.Value);
                    }

                    // Calculate item totals
                    item.subtotal = item.quantity_ordered * item.unit_cost;
                    item.tax_rate = taxRate;
                    item.tax_amount = item.subtotal * taxRate;
                    item.total = item.subtotal + item.tax_amount;

                    // Accumulate order totals
                    orderSubtotal += item.subtotal;
                    orderTaxAmount += item.tax_amount;
                    orderTotal += item.total;
                }

                // Calculate total amount (sum of all item totals)
                decimal totalAmount = orderTotal;

                // Check if subtotal and tax_amount columns exist in PO header
                bool hasSubtotal = await ColumnExistsAsync(connection, transaction, "tbl_purchase_order", "subtotal");
                bool hasTaxAmount = await ColumnExistsAsync(connection, transaction, "tbl_purchase_order", "tax_amount");

                // Build INSERT statement dynamically
                var insertColumns = new List<string> { "supplier_id", "po_number", "order_date", "expected_date", "status", "total_amount", "notes", "created_date" };
                var insertValues = new List<string> { "@supplier_id", "@po_number", "@order_date", "@expected_date", "@status", "@total_amount", "@notes", "@created_date" };
                
                if (hasSubtotal)
                {
                    insertColumns.Add("subtotal");
                    insertValues.Add("@subtotal");
                }
                if (hasTaxAmount)
                {
                    insertColumns.Add("tax_amount");
                    insertValues.Add("@tax_amount");
                }

                var insertSql = $@"
                    INSERT INTO tbl_purchase_order ({string.Join(", ", insertColumns)})
                    VALUES ({string.Join(", ", insertValues)});
                    SELECT SCOPE_IDENTITY();";

                // Step 1: Insert Purchase Order header
                var insertPOCommand = new SqlCommand(insertSql, connection, transaction);

                insertPOCommand.Parameters.AddWithValue("@supplier_id", supplierId);
                insertPOCommand.Parameters.AddWithValue("@po_number", poNumber);
                insertPOCommand.Parameters.AddWithValue("@order_date", orderDate);
                insertPOCommand.Parameters.AddWithValue("@expected_date", expectedDate);
                insertPOCommand.Parameters.AddWithValue("@status", "Pending");
                insertPOCommand.Parameters.AddWithValue("@total_amount", totalAmount);
                insertPOCommand.Parameters.AddWithValue("@notes", (object)notes ?? DBNull.Value);
                insertPOCommand.Parameters.AddWithValue("@created_date", DateTime.Now);
                
                if (hasSubtotal)
                {
                    insertPOCommand.Parameters.AddWithValue("@subtotal", orderSubtotal);
                }
                if (hasTaxAmount)
                {
                    insertPOCommand.Parameters.AddWithValue("@tax_amount", orderTaxAmount);
                }

                var poId = Convert.ToInt32(await insertPOCommand.ExecuteScalarAsync());

                // Step 2: Insert Purchase Order items with tax calculations
                foreach (var item in items)
                {
                    // Check if tax columns exist
                    bool itemHasTaxRate = await ColumnExistsAsync(connection, transaction, "tbl_purchase_order_items", "tax_rate");
                    bool itemHasTaxAmount = await ColumnExistsAsync(connection, transaction, "tbl_purchase_order_items", "tax_amount");
                    bool itemHasSubtotal = await ColumnExistsAsync(connection, transaction, "tbl_purchase_order_items", "subtotal");
                    bool itemHasTotal = await ColumnExistsAsync(connection, transaction, "tbl_purchase_order_items", "total");
                    bool itemHasCreatedDate = await ColumnExistsAsync(connection, transaction, "tbl_purchase_order_items", "created_date");
                    
                    // Build dynamic INSERT based on column existence
                    var itemInsertColumns = new List<string> { "po_id", "product_id", "quantity_ordered", "unit_cost" };
                    var itemInsertValues = new List<string> { "@po_id", "@product_id", "@quantity_ordered", "@unit_cost" };
                    
                    if (itemHasTaxRate)
                    {
                        itemInsertColumns.Add("tax_rate");
                        itemInsertValues.Add("@tax_rate");
                    }
                    if (itemHasTaxAmount)
                    {
                        itemInsertColumns.Add("tax_amount");
                        itemInsertValues.Add("@tax_amount");
                    }
                    if (itemHasSubtotal)
                    {
                        itemInsertColumns.Add("subtotal");
                        itemInsertValues.Add("@subtotal");
                    }
                    if (itemHasTotal)
                    {
                        itemInsertColumns.Add("total");
                        itemInsertValues.Add("@total");
                    }
                    if (itemHasCreatedDate)
                    {
                        itemInsertColumns.Add("created_date");
                        itemInsertValues.Add("@created_date");
                    }

                    var itemInsertSql = $@"
                        INSERT INTO tbl_purchase_order_items ({string.Join(", ", itemInsertColumns)})
                        VALUES ({string.Join(", ", itemInsertValues)})";

                    var insertItemCommand = new SqlCommand(itemInsertSql, connection, transaction);

                    insertItemCommand.Parameters.AddWithValue("@po_id", poId);
                    insertItemCommand.Parameters.AddWithValue("@product_id", item.product_id);
                    insertItemCommand.Parameters.AddWithValue("@quantity_ordered", item.quantity_ordered);
                    insertItemCommand.Parameters.AddWithValue("@unit_cost", item.unit_cost);
                    
                    if (itemHasTaxRate)
                    {
                        insertItemCommand.Parameters.AddWithValue("@tax_rate", item.tax_rate);
                    }
                    if (itemHasTaxAmount)
                    {
                        insertItemCommand.Parameters.AddWithValue("@tax_amount", item.tax_amount);
                    }
                    if (itemHasSubtotal)
                    {
                        insertItemCommand.Parameters.AddWithValue("@subtotal", item.subtotal);
                    }
                    if (itemHasTotal)
                    {
                        insertItemCommand.Parameters.AddWithValue("@total", item.total);
                    }
                    if (itemHasCreatedDate)
                    {
                        insertItemCommand.Parameters.AddWithValue("@created_date", DateTime.Now);
                    }

                    await insertItemCommand.ExecuteNonQueryAsync();
                }

                // Commit transaction
                transaction.Commit();

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_purchase_order",
                        poId,
                        null,
                        new { po_number = poNumber, supplier_id = supplierId, order_date = orderDate, expected_date = expectedDate, status = "Pending", total_amount = totalAmount, item_count = items.Count },
                        null,
                        null,
                        $"created purchase order: {poNumber}"
                    );
                }

                return poId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error creating purchase order: {ex.Message}");
            }
        }

        // Generate PO Number (PO-YYYYMM-NNNN format, e.g., PO-202501-0001)
        private async Task<string> GeneratePONumberAsync(SqlConnection connection, SqlTransaction transaction, DateTime orderDate)
        {
            // Get year and month from order date
            int year = orderDate.Year;
            int month = orderDate.Month;
            string yearMonth = $"{year}{month.ToString("D2")}"; // YYYYMM format
            
            // Find the highest PO number for this year-month
            var command = new SqlCommand(@"
                SELECT ISNULL(MAX(CAST(SUBSTRING(po_number, 10, 4) AS INT)), 0)
                FROM tbl_purchase_order
                WHERE po_number LIKE @pattern
                AND LEN(po_number) = 13
                AND ISNUMERIC(SUBSTRING(po_number, 10, 4)) = 1", connection, transaction);
            
            string pattern = $"PO-{yearMonth}-____";
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
            
            // Generate PO number and ensure uniqueness (handle race conditions)
            string poNumber;
            int maxAttempts = 100; // Prevent infinite loop
            int attempts = 0;
            
            do
            {
                // Generate PO number: PO-YYYYMM-NNNN
                poNumber = $"PO-{yearMonth}-{nextNumber.ToString("D4")}";
                
                // Check for uniqueness within the transaction
                var checkCommand = new SqlCommand(
                    "SELECT COUNT(*) FROM tbl_purchase_order WHERE po_number = @po_number",
                    connection, transaction);
                checkCommand.Parameters.AddWithValue("@po_number", poNumber);
                
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
                    throw new Exception($"Unable to generate unique PO number after {maxAttempts} attempts. Please try again.");
                }
                
            } while (true);
            
            return poNumber;
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
    }
}

