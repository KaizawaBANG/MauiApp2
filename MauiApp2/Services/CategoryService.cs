using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<int> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int categoryId);
        Task<Category> GetCategoryByIdAsync(int categoryId);
    }
    public class CategoryService : ICategoryService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public CategoryService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // READ - Get all categories
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var categories = new List<Category>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT category_id, category_name, ISNULL(category_code, ''), description 
                    FROM tbl_category 
                    ORDER BY category_name", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new Category
                    {
                        category_id = reader.GetInt32(0),
                        category_name = reader.GetString(1),
                        category_code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        description = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_category'"))
                {
                    Console.WriteLine("tbl_category table doesn't exist yet.");
                    return categories;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading categories: {ex.Message}");
            }

            return categories;
        }

        // CREATE - Add new category
        public async Task<int> CreateCategoryAsync(Category category)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Auto-generate category_code if not provided
                string categoryCode = category.category_code;
                if (string.IsNullOrWhiteSpace(categoryCode))
                {
                    // Generate code from first 4 characters of category name
                    var sanitized = System.Text.RegularExpressions.Regex.Replace(
                        category.category_name.ToUpper(), @"[^A-Z0-9]", "");
                    categoryCode = sanitized.Length > 4 ? sanitized.Substring(0, 4) : (sanitized.Length > 0 ? sanitized : "MISC");
                }

                var command = new SqlCommand(@"
                    INSERT INTO tbl_category (category_name, category_code, description)
                    VALUES (@category_name, @category_code, @description);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@category_name", category.category_name);
                command.Parameters.AddWithValue("@category_code", categoryCode);
                command.Parameters.AddWithValue("@description", (object)category.description ?? DBNull.Value);

                var result = await command.ExecuteScalarAsync();
                var categoryId = Convert.ToInt32(result);

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_category",
                        categoryId,
                        null,
                        new { category_name = category.category_name, category_code = categoryCode, description = category.description },
                        null,
                        null,
                        $"added new category: {category.category_name}"
                    );
                }

                return categoryId;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_category'"))
                {
                    throw new Exception("tbl_category table doesn't exist. Please create the table first.");
                }
                throw new Exception($"Error creating category: {ex.Message}");
            }
        }

        // UPDATE - Modify existing category
        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            try
            {
                // Get old values for audit log
                Category? oldCategory = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    oldCategory = await GetCategoryByIdAsync(category.category_id);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Auto-generate category_code if not provided
                string categoryCode = category.category_code;
                if (string.IsNullOrWhiteSpace(categoryCode))
                {
                    // Generate code from first 4 characters of category name
                    var sanitized = System.Text.RegularExpressions.Regex.Replace(
                        category.category_name.ToUpper(), @"[^A-Z0-9]", "");
                    categoryCode = sanitized.Length > 4 ? sanitized.Substring(0, 4) : (sanitized.Length > 0 ? sanitized : "MISC");
                }

                var command = new SqlCommand(@"
                    UPDATE tbl_category 
                    SET category_name = @category_name, category_code = @category_code, description = @description
                    WHERE category_id = @category_id", connection);

                command.Parameters.AddWithValue("@category_id", category.category_id);
                command.Parameters.AddWithValue("@category_name", category.category_name);
                command.Parameters.AddWithValue("@category_code", categoryCode);
                command.Parameters.AddWithValue("@description", (object)category.description ?? DBNull.Value);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldCategory != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_category",
                        category.category_id,
                        new { category_name = oldCategory.category_name, category_code = oldCategory.category_code, description = oldCategory.description },
                        new { category_name = category.category_name, category_code = categoryCode, description = category.description },
                        null,
                        null,
                        $"updated category: {category.category_name}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating category: {ex.Message}");
            }
        }

        // DELETE - Remove category
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            try
            {
                // Get category details for audit log before deletion
                Category? categoryToDelete = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    categoryToDelete = await GetCategoryByIdAsync(categoryId);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // First check if there are any products using this category
                var checkCommand = new SqlCommand(
                    "SELECT COUNT(*) FROM tbl_product WHERE category_id = @CategoryId",
                    connection);
                checkCommand.Parameters.AddWithValue("@CategoryId", categoryId);

                var productCount = (int)await checkCommand.ExecuteScalarAsync();

                if (productCount > 0)
                {
                    throw new InvalidOperationException("Cannot delete category because there are products associated with it.");
                }

                var deleteCommand = new SqlCommand("DELETE FROM tbl_category WHERE category_id = @category_id", connection);
                deleteCommand.Parameters.AddWithValue("@category_id", categoryId);

                var success = await deleteCommand.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && categoryToDelete != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Delete",
                        "tbl_category",
                        categoryId,
                        new { category_name = categoryToDelete.category_name, category_code = categoryToDelete.category_code, description = categoryToDelete.description },
                        null,
                        null,
                        null,
                        $"deleted category: {categoryToDelete.category_name}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting category: {ex.Message}");
            }
        }

        // READ - Get category by ID
        public async Task<Category> GetCategoryByIdAsync(int categoryId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT category_id, category_name, ISNULL(category_code, ''), description 
                    FROM tbl_category 
                    WHERE category_id = @category_id", connection);

                command.Parameters.AddWithValue("@category_id", categoryId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Category
                    {
                        category_id = reader.GetInt32(0),
                        category_name = reader.GetString(1),
                        category_code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        description = reader.IsDBNull(3) ? null : reader.GetString(3)
                    };
                }

                return new Category(); // Return empty category instead of null
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading category: {ex.Message}");
            }
        }
    }
}