using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IBrandService
    {
        Task<List<Brand>> GetBrandsAsync();
        Task<int> CreateBrandAsync(Brand brand);
        Task<bool> UpdateBrandAsync(Brand brand);
        Task<bool> DeleteBrandAsync(int brandId);
        Task<Brand> GetBrandByIdAsync(int brandId);
    }

    public class BrandService : IBrandService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public BrandService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // READ - Get all brands
        public async Task<List<Brand>> GetBrandsAsync()
        {
            var brands = new List<Brand>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT brand_id, brand_name, ISNULL(brand_code, ''), description 
                    FROM tbl_brand 
                    ORDER BY brand_name", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    brands.Add(new Brand
                    {
                        brand_id = reader.GetInt32(0),
                        brand_name = reader.GetString(1),
                        brand_code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        description = reader.IsDBNull(3) ? null : reader.GetString(3)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_brand'"))
                {
                    Console.WriteLine("tbl_brand table doesn't exist yet.");
                    return brands;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading brands: {ex.Message}");
            }

            return brands;
        }

        // CREATE - Add new brand
        public async Task<int> CreateBrandAsync(Brand brand)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Auto-generate brand_code if not provided
                string brandCode = brand.brand_code;
                if (string.IsNullOrWhiteSpace(brandCode))
                {
                    // Generate code from first 3 characters of brand name
                    var sanitized = System.Text.RegularExpressions.Regex.Replace(
                        brand.brand_name.ToUpper(), @"[^A-Z0-9]", "");
                    brandCode = sanitized.Length > 3 ? sanitized.Substring(0, 3) : (sanitized.Length > 0 ? sanitized : "GEN");
                }

                var command = new SqlCommand(@"
                    INSERT INTO tbl_brand (brand_name, brand_code, description)
                    VALUES (@brand_name, @brand_code, @description);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@brand_name", brand.brand_name);
                command.Parameters.AddWithValue("@brand_code", brandCode);
                command.Parameters.AddWithValue("@description", (object)brand.description ?? DBNull.Value);

                var result = await command.ExecuteScalarAsync();
                var brandId = Convert.ToInt32(result);

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_brand",
                        brandId,
                        null,
                        new { brand_name = brand.brand_name, brand_code = brandCode, description = brand.description },
                        null,
                        null,
                        $"added new brand: {brand.brand_name}"
                    );
                }

                return brandId;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_brand'"))
                {
                    throw new Exception("tbl_brand table doesn't exist. Please create the table first.");
                }
                throw new Exception($"Error creating brand: {ex.Message}");
            }
        }

        // UPDATE - Modify existing brand
        public async Task<bool> UpdateBrandAsync(Brand brand)
        {
            try
            {
                // Get old values for audit log
                Brand? oldBrand = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    oldBrand = await GetBrandByIdAsync(brand.brand_id);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Auto-generate brand_code if not provided
                string brandCode = brand.brand_code;
                if (string.IsNullOrWhiteSpace(brandCode))
                {
                    // Generate code from first 3 characters of brand name
                    var sanitized = System.Text.RegularExpressions.Regex.Replace(
                        brand.brand_name.ToUpper(), @"[^A-Z0-9]", "");
                    brandCode = sanitized.Length > 3 ? sanitized.Substring(0, 3) : (sanitized.Length > 0 ? sanitized : "GEN");
                }

                var command = new SqlCommand(@"
                    UPDATE tbl_brand 
                    SET brand_name = @brand_name, brand_code = @brand_code, description = @description
                    WHERE brand_id = @brand_id", connection);

                command.Parameters.AddWithValue("@brand_id", brand.brand_id);
                command.Parameters.AddWithValue("@brand_name", brand.brand_name);
                command.Parameters.AddWithValue("@brand_code", brandCode);
                command.Parameters.AddWithValue("@description", (object)brand.description ?? DBNull.Value);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldBrand != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_brand",
                        brand.brand_id,
                        new { brand_name = oldBrand.brand_name, brand_code = oldBrand.brand_code, description = oldBrand.description },
                        new { brand_name = brand.brand_name, brand_code = brandCode, description = brand.description },
                        null,
                        null,
                        $"updated brand: {brand.brand_name}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating brand: {ex.Message}");
            }
        }

        // DELETE - Remove brand
        public async Task<bool> DeleteBrandAsync(int brandId)
        {
            try
            {
                // Get brand details for audit log before deletion
                Brand? brandToDelete = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    brandToDelete = await GetBrandByIdAsync(brandId);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand("DELETE FROM tbl_brand WHERE brand_id = @brand_id", connection);
                command.Parameters.AddWithValue("@brand_id", brandId);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && brandToDelete != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Delete",
                        "tbl_brand",
                        brandId,
                        new { brand_name = brandToDelete.brand_name, brand_code = brandToDelete.brand_code, description = brandToDelete.description },
                        null,
                        null,
                        null,
                        $"deleted brand: {brandToDelete.brand_name}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting brand: {ex.Message}");
            }
        }

        // READ - Get brand by ID
        public async Task<Brand> GetBrandByIdAsync(int brandId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT brand_id, brand_name, ISNULL(brand_code, ''), description 
                    FROM tbl_brand 
                    WHERE brand_id = @brand_id", connection);

                command.Parameters.AddWithValue("@brand_id", brandId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Brand
                    {
                        brand_id = reader.GetInt32(0),
                        brand_name = reader.GetString(1),
                        brand_code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        description = reader.IsDBNull(3) ? null : reader.GetString(3)
                    };
                }

                return new Brand(); // Return empty brand instead of null
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading brand: {ex.Message}");
            }
        }
    }
}