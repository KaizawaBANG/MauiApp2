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
                return Convert.ToInt32(result);
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

                return await command.ExecuteNonQueryAsync() > 0;
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

                return await deleteCommand.ExecuteNonQueryAsync() > 0;
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