using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface ITaxService
    {
        Task<List<Tax>> GetTaxesAsync();
        Task<int> CreateTaxAsync(Tax tax);
        Task<bool> UpdateTaxAsync(Tax tax);
        Task<bool> DeleteTaxAsync(int taxId);
        Task<Tax> GetTaxByIdAsync(int taxId);
    }

    public class TaxService : ITaxService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public TaxService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // READ - Get all taxes
        public async Task<List<Tax>> GetTaxesAsync()
        {
            var taxes = new List<Tax>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT tax_id, tax_name, tax_type, tax_rate, is_active, created_date 
                    FROM tbl_tax 
                    ORDER BY tax_name", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    taxes.Add(new Tax
                    {
                        tax_id = reader.GetInt32(0),
                        tax_name = reader.GetString(1),
                        tax_type = reader.GetString(2),
                        tax_rate = reader.GetDecimal(3),
                        is_active = reader.GetBoolean(4),
                        created_date = reader.GetDateTime(5)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_tax'"))
                {
                    Console.WriteLine("tbl_tax table doesn't exist yet.");
                    return taxes;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading taxes: {ex.Message}");
            }

            return taxes;
        }

        // CREATE - Add new tax
        public async Task<int> CreateTaxAsync(Tax tax)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    INSERT INTO tbl_tax (tax_name, tax_type, tax_rate, is_active, created_date)
                    VALUES (@tax_name, @tax_type, @tax_rate, @is_active, @created_date);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@tax_name", tax.tax_name);
                command.Parameters.AddWithValue("@tax_type", tax.tax_type);
                command.Parameters.AddWithValue("@tax_rate", tax.tax_rate);
                command.Parameters.AddWithValue("@is_active", tax.is_active);
                command.Parameters.AddWithValue("@created_date", tax.created_date != default(DateTime) ? tax.created_date : DateTime.Now);

                var result = await command.ExecuteScalarAsync();
                var taxId = Convert.ToInt32(result);

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_tax",
                        taxId,
                        null,
                        new { tax_name = tax.tax_name, tax_type = tax.tax_type, tax_rate = tax.tax_rate, is_active = tax.is_active },
                        null,
                        null,
                        $"added new tax: {tax.tax_name}"
                    );
                }

                return taxId;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_tax'"))
                {
                    throw new Exception("tbl_tax table doesn't exist. Please create the table first.");
                }
                throw new Exception($"Error creating tax: {ex.Message}");
            }
        }

        // UPDATE - Modify existing tax
        public async Task<bool> UpdateTaxAsync(Tax tax)
        {
            try
            {
                // Get old values for audit log
                Tax? oldTax = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    oldTax = await GetTaxByIdAsync(tax.tax_id);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_tax 
                    SET tax_name = @tax_name, 
                        tax_type = @tax_type,
                        tax_rate = @tax_rate,
                        is_active = @is_active
                    WHERE tax_id = @tax_id", connection);

                command.Parameters.AddWithValue("@tax_id", tax.tax_id);
                command.Parameters.AddWithValue("@tax_name", tax.tax_name);
                command.Parameters.AddWithValue("@tax_type", tax.tax_type);
                command.Parameters.AddWithValue("@tax_rate", tax.tax_rate);
                command.Parameters.AddWithValue("@is_active", tax.is_active);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldTax != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_tax",
                        tax.tax_id,
                        new { tax_name = oldTax.tax_name, tax_type = oldTax.tax_type, tax_rate = oldTax.tax_rate, is_active = oldTax.is_active },
                        new { tax_name = tax.tax_name, tax_type = tax.tax_type, tax_rate = tax.tax_rate, is_active = tax.is_active },
                        null,
                        null,
                        $"updated tax: {tax.tax_name}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating tax: {ex.Message}");
            }
        }

        // DELETE - Remove tax
        public async Task<bool> DeleteTaxAsync(int taxId)
        {
            try
            {
                // Get tax details for audit log before deletion
                Tax? taxToDelete = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    taxToDelete = await GetTaxByIdAsync(taxId);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var deleteCommand = new SqlCommand("DELETE FROM tbl_tax WHERE tax_id = @tax_id", connection);
                deleteCommand.Parameters.AddWithValue("@tax_id", taxId);

                var success = await deleteCommand.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && taxToDelete != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Delete",
                        "tbl_tax",
                        taxId,
                        new { tax_name = taxToDelete.tax_name, tax_type = taxToDelete.tax_type, tax_rate = taxToDelete.tax_rate, is_active = taxToDelete.is_active },
                        null,
                        null,
                        null,
                        $"deleted tax: {taxToDelete.tax_name}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting tax: {ex.Message}");
            }
        }

        // READ - Get tax by ID
        public async Task<Tax> GetTaxByIdAsync(int taxId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT tax_id, tax_name, tax_type, tax_rate, is_active, created_date 
                    FROM tbl_tax 
                    WHERE tax_id = @tax_id", connection);

                command.Parameters.AddWithValue("@tax_id", taxId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Tax
                    {
                        tax_id = reader.GetInt32(0),
                        tax_name = reader.GetString(1),
                        tax_type = reader.GetString(2),
                        tax_rate = reader.GetDecimal(3),
                        is_active = reader.GetBoolean(4),
                        created_date = reader.GetDateTime(5)
                    };
                }

                return new Tax(); // Return empty tax instead of null
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading tax: {ex.Message}");
            }
        }
    }
}
