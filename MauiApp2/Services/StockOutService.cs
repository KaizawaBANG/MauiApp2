using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IStockOutService
    {
        Task<int> CreateStockOutFromSaleAsync(SqlConnection connection, SqlTransaction transaction, int salesOrderId, List<StockOutItem> items, int userId);
        Task<List<StockOut>> GetStockOutHistoryAsync();
        Task<StockOut> GetStockOutByIdAsync(int stockOutId);
        Task<List<StockOutItem>> GetStockOutItemsAsync(int stockOutId);
    }

    public class StockOutService : IStockOutService
    {
        // Create Stock Out from Sale (called within Sales Order transaction)
        public async Task<int> CreateStockOutFromSaleAsync(SqlConnection connection, SqlTransaction transaction, int salesOrderId, List<StockOutItem> items, int userId)
        {
            // Generate Stock Out Number (STO-001, STO-002, etc.)
            string stockOutNumber = await GenerateStockOutNumberAsync(connection, transaction);

            // Create Stock Out header
            var stockOutId = await CreateStockOutHeaderAsync(connection, transaction, salesOrderId, stockOutNumber, userId);

            // Create Stock Out items
            foreach (var item in items)
            {
                await CreateStockOutItemAsync(connection, transaction, stockOutId, item);
            }

            return stockOutId;
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
        private async Task<int> CreateStockOutHeaderAsync(SqlConnection connection, SqlTransaction transaction, int salesOrderId, string stockOutNumber, int userId)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_stock_out (sales_order_id, stock_out_number, stock_out_date, reason, processed_by, created_date)
                VALUES (@sales_order_id, @stock_out_number, @stock_out_date, @reason, @processed_by, @created_date);
                SELECT SCOPE_IDENTITY();", connection, transaction);

            command.Parameters.AddWithValue("@sales_order_id", salesOrderId);
            command.Parameters.AddWithValue("@stock_out_number", stockOutNumber);
            command.Parameters.AddWithValue("@stock_out_date", DateTime.Now);
            command.Parameters.AddWithValue("@reason", "Sale");
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
    }
}





