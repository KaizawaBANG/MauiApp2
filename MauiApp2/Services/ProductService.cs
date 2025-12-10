using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IProductService
    {
        Task<List<Product>> GetProductsAsync();
        Task<int> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int productId);
        Task<Product> GetProductByIdAsync(int productId);
        Task<Product> GetProductBySkuAsync(string sku);
    }

    public class ProductService : IProductService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public ProductService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // READ - Get all products
        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT product_id, brand_id, category_id, tax_id, product_name, product_sku, 
                           model_number, cost_price, sell_price, quantity, status, 
                           created_date, modified_date 
                    FROM tbl_product 
                    ORDER BY created_date DESC, product_id DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    products.Add(new Product
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
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_product'"))
                {
                    Console.WriteLine("tbl_product table doesn't exist yet.");
                    return products;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading products: {ex.Message}");
            }

            return products;
        }

        // Generate unique SKU in format: CATEGORY-BRAND-NUMBER (e.g., REF-SAM-0001)
        private async Task<string> GenerateSkuAsync(SqlConnection connection, int? brandId, int? categoryId)
        {
            // Get category code
            string categoryCode = "MISC"; // Default if no category
            if (categoryId.HasValue)
            {
                var categoryCommand = new SqlCommand(
                    "SELECT ISNULL(category_code, UPPER(LEFT(REPLACE(REPLACE(category_name, ' ', ''), '-', ''), 4))) FROM tbl_category WHERE category_id = @category_id",
                    connection);
                categoryCommand.Parameters.AddWithValue("@category_id", categoryId.Value);
                var categoryResult = await categoryCommand.ExecuteScalarAsync();
                if (categoryResult != null && !DBNull.Value.Equals(categoryResult))
                {
                    var code = categoryResult.ToString() ?? "MISC";
                    categoryCode = code.Length > 4 ? code.Substring(0, 4) : code;
                }
            }

            // Get brand code
            string brandCode = "GEN"; // Default if no brand
            if (brandId.HasValue)
            {
                var brandCommand = new SqlCommand(
                    "SELECT ISNULL(brand_code, UPPER(LEFT(REPLACE(REPLACE(brand_name, ' ', ''), '-', ''), 3))) FROM tbl_brand WHERE brand_id = @brand_id",
                    connection);
                brandCommand.Parameters.AddWithValue("@brand_id", brandId.Value);
                var brandResult = await brandCommand.ExecuteScalarAsync();
                if (brandResult != null && !DBNull.Value.Equals(brandResult))
                {
                    var code = brandResult.ToString() ?? "GEN";
                    brandCode = code.Length > 3 ? code.Substring(0, 3) : code;
                }
            }

            // Ensure codes are uppercase and alphanumeric only
            categoryCode = Regex.Replace(categoryCode.ToUpper(), @"[^A-Z0-9]", "");
            brandCode = Regex.Replace(brandCode.ToUpper(), @"[^A-Z0-9]", "");

            if (string.IsNullOrWhiteSpace(categoryCode)) categoryCode = "MISC";
            if (string.IsNullOrWhiteSpace(brandCode)) brandCode = "GEN";

            // Get the next sequential number for this category-brand combination
            int nextNumber = 1;
            var prefix = $"{categoryCode}-{brandCode}-";
            var maxSkuCommand = new SqlCommand(@"
                SELECT MAX(CAST(SUBSTRING(product_sku, @prefixLen + 1, 4) AS INT))
                FROM tbl_product
                WHERE product_sku LIKE @pattern 
                AND LEN(product_sku) = @prefixLen + 4
                AND ISNUMERIC(SUBSTRING(product_sku, @prefixLen + 1, 4)) = 1", connection);
            
            string pattern = prefix + "____";
            maxSkuCommand.Parameters.AddWithValue("@pattern", pattern);
            maxSkuCommand.Parameters.AddWithValue("@prefixLen", prefix.Length);
            
            var maxResult = await maxSkuCommand.ExecuteScalarAsync();
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

            // Generate SKU format: CATEGORY-BRAND-NUMBER
            string sku = $"{categoryCode}-{brandCode}-{nextNumber.ToString("D4")}";

            // Double-check for uniqueness (in case of race condition)
            var checkCommand = new SqlCommand(
                "SELECT COUNT(*) FROM tbl_product WHERE product_sku = @product_sku",
                connection);
            checkCommand.Parameters.AddWithValue("@product_sku", sku);

            var count = (int)await checkCommand.ExecuteScalarAsync();
            if (count > 0)
            {
                // If exists, increment and try again
                nextNumber++;
                sku = $"{categoryCode}-{brandCode}-{nextNumber.ToString("D4")}";
            }

            return sku;
        }

        // Sanitize string for SKU: remove spaces, special chars, uppercase
        private string SanitizeForSku(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "GEN";

            // Remove all non-alphanumeric characters except dashes, convert to uppercase
            var sanitized = Regex.Replace(input.ToUpper(), @"[^A-Z0-9-]", "");

            // Remove multiple consecutive dashes
            sanitized = Regex.Replace(sanitized, @"-+", "-");

            // Remove leading/trailing dashes
            sanitized = sanitized.Trim('-');

            // Limit length to 10 characters for brand/category
            if (sanitized.Length > 10)
                sanitized = sanitized.Substring(0, 10);

            return string.IsNullOrWhiteSpace(sanitized) ? "GEN" : sanitized;
        }

        // CREATE - Add new product
        public async Task<int> CreateProductAsync(Product product)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Auto-generate SKU if not provided or empty
                string sku = product.product_sku;
                if (string.IsNullOrWhiteSpace(sku))
                {
                    sku = await GenerateSkuAsync(connection, product.brand_id, product.category_id);
                }
                else
                {
                    // Check if provided SKU already exists
                    var checkCommand = new SqlCommand(
                        "SELECT COUNT(*) FROM tbl_product WHERE product_sku = @product_sku",
                        connection);
                    checkCommand.Parameters.AddWithValue("@product_sku", sku);
                    var count = (int)await checkCommand.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        throw new Exception($"A product with SKU '{sku}' already exists.");
                    }
                }

                var command = new SqlCommand(@"
                    INSERT INTO tbl_product (brand_id, category_id, tax_id, product_name, product_sku, 
                                             model_number, cost_price, sell_price, quantity, 
                                             status, created_date, modified_date)
                    VALUES (@brand_id, @category_id, @tax_id, @product_name, @product_sku, 
                            @model_number, @cost_price, @sell_price, @quantity, 
                            @status, @created_date, @modified_date);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@brand_id", (object)product.brand_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@category_id", (object)product.category_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@tax_id", (object)product.tax_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@product_name", product.product_name);
                command.Parameters.AddWithValue("@product_sku", sku);
                command.Parameters.AddWithValue("@model_number", (object)product.model_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@cost_price", (object)product.cost_price ?? DBNull.Value);
                command.Parameters.AddWithValue("@sell_price", product.sell_price);
                command.Parameters.AddWithValue("@quantity", (object)product.quantity ?? DBNull.Value);
                command.Parameters.AddWithValue("@status", (object)product.status ?? DBNull.Value);
                command.Parameters.AddWithValue("@created_date", product.created_date ?? DateTime.Now);
                command.Parameters.AddWithValue("@modified_date", product.modified_date ?? DateTime.Now);

                var result = await command.ExecuteScalarAsync();
                int productId = Convert.ToInt32(result);

                // Update the product object with the generated SKU
                product.product_sku = sku;

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_product",
                        productId,
                        null,
                        new { product_name = product.product_name, product_sku = sku, sell_price = product.sell_price },
                        null,
                        null,
                        $"added new product: {product.product_name}"
                    );
                }

                return productId;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_product'"))
                {
                    throw new Exception("tbl_product table doesn't exist. Please create the table first.");
                }
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key"))
                {
                    throw new Exception($"A product with SKU '{product.product_sku}' already exists.");
                }
                throw new Exception($"Error creating product: {ex.Message}");
            }
        }

        // UPDATE - Modify existing product
        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                // Get old values for audit log
                Product? oldProduct = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    oldProduct = await GetProductByIdAsync(product.product_id);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_product 
                    SET brand_id = @brand_id, 
                        category_id = @category_id, 
                        tax_id = @tax_id,
                        product_name = @product_name, 
                        product_sku = @product_sku, 
                        model_number = @model_number, 
                        cost_price = @cost_price, 
                        sell_price = @sell_price, 
                        quantity = @quantity, 
                        status = @status, 
                        modified_date = @modified_date
                    WHERE product_id = @product_id", connection);

                command.Parameters.AddWithValue("@product_id", product.product_id);
                command.Parameters.AddWithValue("@brand_id", (object)product.brand_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@category_id", (object)product.category_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@tax_id", (object)product.tax_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@product_name", product.product_name);
                command.Parameters.AddWithValue("@product_sku", product.product_sku);
                command.Parameters.AddWithValue("@model_number", (object)product.model_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@cost_price", (object)product.cost_price ?? DBNull.Value);
                command.Parameters.AddWithValue("@sell_price", product.sell_price);
                command.Parameters.AddWithValue("@quantity", (object)product.quantity ?? DBNull.Value);
                command.Parameters.AddWithValue("@status", (object)product.status ?? DBNull.Value);
                command.Parameters.AddWithValue("@modified_date", DateTime.Now);

                bool success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldProduct != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_product",
                        product.product_id,
                        new { product_name = oldProduct.product_name, product_sku = oldProduct.product_sku, sell_price = oldProduct.sell_price, quantity = oldProduct.quantity },
                        new { product_name = product.product_name, product_sku = product.product_sku, sell_price = product.sell_price, quantity = product.quantity },
                        null,
                        null,
                        $"updated product: {product.product_name}"
                    );
                }

                return success;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key"))
                {
                    throw new Exception($"A product with SKU '{product.product_sku}' already exists.");
                }
                throw new Exception($"Error updating product: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating product: {ex.Message}");
            }
        }

        // DELETE - Remove product
        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                // Get product details for audit log before deletion
                Product? productToDelete = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    productToDelete = await GetProductByIdAsync(productId);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var deleteCommand = new SqlCommand("DELETE FROM tbl_product WHERE product_id = @product_id", connection);
                deleteCommand.Parameters.AddWithValue("@product_id", productId);

                bool success = await deleteCommand.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && productToDelete != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Delete",
                        "tbl_product",
                        productId,
                        new { product_name = productToDelete.product_name, product_sku = productToDelete.product_sku, sell_price = productToDelete.sell_price },
                        null,
                        null,
                        null,
                        $"deleted product: {productToDelete.product_name}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting product: {ex.Message}");
            }
        }

        // READ - Get product by ID
        public async Task<Product> GetProductByIdAsync(int productId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT product_id, brand_id, category_id, tax_id, product_name, product_sku, 
                           model_number, cost_price, sell_price, quantity, status, 
                           created_date, modified_date 
                    FROM tbl_product 
                    WHERE product_id = @product_id", connection);

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

                return new Product(); // Return empty product instead of null
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading product: {ex.Message}");
            }
        }

        // READ - Get product by SKU
        public async Task<Product> GetProductBySkuAsync(string sku)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT product_id, brand_id, category_id, tax_id, product_name, product_sku, 
                           model_number, cost_price, sell_price, quantity, status, 
                           created_date, modified_date 
                    FROM tbl_product 
                    WHERE product_sku = @product_sku", connection);

                command.Parameters.AddWithValue("@product_sku", sku);

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

                return new Product(); // Return empty product instead of null
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading product: {ex.Message}");
            }
        }
    }
}