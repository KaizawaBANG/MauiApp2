using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface ISupplierService
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<int> CreateSupplierAsync(Supplier supplier);
        Task<bool> UpdateSupplierAsync(Supplier supplier);
        Task<bool> DeleteSupplierAsync(int supplierId);
        Task<Supplier> GetSupplierByIdAsync(int supplierId);
    }

    public class SupplierService : ISupplierService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public SupplierService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // READ - Get all suppliers
        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            var suppliers = new List<Supplier>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT supplier_id, supplier_name, contact_number, email, is_active, created_date, modified_date
                    FROM tbl_supplier
                    ORDER BY supplier_name", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    suppliers.Add(new Supplier
                    {
                        supplier_id = reader.GetInt32(0),
                        supplier_name = reader.GetString(1),
                        contact_number = reader.IsDBNull(2) ? null : reader.GetString(2),
                        email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        is_active = reader.FieldCount > 4 && !reader.IsDBNull(4) ? reader.GetBoolean(4) : true,
                        created_date = reader.FieldCount > 5 && !reader.IsDBNull(5) ? reader.GetDateTime(5) : DateTime.Now,
                        modified_date = reader.FieldCount > 6 && !reader.IsDBNull(6) ? (DateTime?)reader.GetDateTime(6) : null
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_supplier'"))
                {
                    Console.WriteLine("tbl_supplier table doesn't exist yet.");
                    return suppliers;
                }
                if (ex.Message.Contains("Invalid column name"))
                {
                    // Columns don't exist yet, fall back to basic columns
                    Console.WriteLine("Additional columns (is_active, created_date, modified_date) don't exist yet. Please run AddMissingColumnsToSupplierTable.sql script.");
                    // Re-throw so user knows to run the script
                    throw new Exception("Supplier table is missing required columns. Please run the AddMissingColumnsToSupplierTable.sql script to add is_active, created_date, and modified_date columns.");
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading suppliers: {ex.Message}");
            }

            return suppliers;
        }

        // CREATE - Add new supplier
        public async Task<int> CreateSupplierAsync(Supplier supplier)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    INSERT INTO tbl_supplier (supplier_name, contact_number, email, is_active, created_date)
                    VALUES (@supplier_name, @contact_number, @email, @is_active, @created_date);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@supplier_name", supplier.supplier_name);
                command.Parameters.AddWithValue("@contact_number", (object)supplier.contact_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@email", (object)supplier.email ?? DBNull.Value);
                command.Parameters.AddWithValue("@is_active", supplier.is_active);
                command.Parameters.AddWithValue("@created_date", supplier.created_date);

                var result = await command.ExecuteScalarAsync();
                var supplierId = Convert.ToInt32(result);

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_supplier",
                        supplierId,
                        null,
                        new { supplier_name = supplier.supplier_name, contact_number = supplier.contact_number, email = supplier.email, is_active = supplier.is_active },
                        null,
                        null,
                        $"added new supplier: {supplier.supplier_name}"
                    );
                }

                return supplierId;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_supplier'"))
                {
                    throw new Exception("tbl_supplier table doesn't exist. Please create the table first.");
                }
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key"))
                {
                    throw new Exception($"Email already exists. Please use a different email.");
                }
                throw new Exception($"Error creating supplier: {ex.Message}");
            }
        }

        // UPDATE - Modify existing supplier
        public async Task<bool> UpdateSupplierAsync(Supplier supplier)
        {
            try
            {
                // Get old values for audit log
                Supplier? oldSupplier = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    oldSupplier = await GetSupplierByIdAsync(supplier.supplier_id);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_supplier 
                    SET supplier_name = @supplier_name, 
                        contact_number = @contact_number,
                        email = @email,
                        is_active = @is_active,
                        modified_date = @modified_date
                    WHERE supplier_id = @supplier_id", connection);

                command.Parameters.AddWithValue("@supplier_id", supplier.supplier_id);
                command.Parameters.AddWithValue("@supplier_name", supplier.supplier_name);
                command.Parameters.AddWithValue("@contact_number", (object)supplier.contact_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@email", (object)supplier.email ?? DBNull.Value);
                command.Parameters.AddWithValue("@is_active", supplier.is_active);
                command.Parameters.AddWithValue("@modified_date", DateTime.Now);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldSupplier != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_supplier",
                        supplier.supplier_id,
                        new { supplier_name = oldSupplier.supplier_name, contact_number = oldSupplier.contact_number, email = oldSupplier.email, is_active = oldSupplier.is_active },
                        new { supplier_name = supplier.supplier_name, contact_number = supplier.contact_number, email = supplier.email, is_active = supplier.is_active },
                        null,
                        null,
                        $"updated supplier: {supplier.supplier_name}"
                    );
                }

                return success;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key"))
                {
                    throw new Exception($"Email already exists. Please use a different email.");
                }
                throw new Exception($"Error updating supplier: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating supplier: {ex.Message}");
            }
        }

        // DELETE - Remove supplier
        public async Task<bool> DeleteSupplierAsync(int supplierId)
        {
            try
            {
                // Get supplier details for audit log before deletion
                Supplier? supplierToDelete = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    supplierToDelete = await GetSupplierByIdAsync(supplierId);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand("DELETE FROM tbl_supplier WHERE supplier_id = @supplier_id", connection);
                command.Parameters.AddWithValue("@supplier_id", supplierId);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && supplierToDelete != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Delete",
                        "tbl_supplier",
                        supplierId,
                        new { supplier_name = supplierToDelete.supplier_name, contact_number = supplierToDelete.contact_number, email = supplierToDelete.email, is_active = supplierToDelete.is_active },
                        null,
                        null,
                        null,
                        $"deleted supplier: {supplierToDelete.supplier_name}"
                    );
                }

                return success;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("FOREIGN KEY constraint"))
                {
                    throw new Exception("Cannot delete supplier. This supplier is being used in purchase orders.");
                }
                throw new Exception($"Error deleting supplier: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting supplier: {ex.Message}");
            }
        }

        // READ - Get supplier by ID
        public async Task<Supplier> GetSupplierByIdAsync(int supplierId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT supplier_id, supplier_name, contact_number, email, is_active, created_date, modified_date
                    FROM tbl_supplier
                    WHERE supplier_id = @supplier_id", connection);

                command.Parameters.AddWithValue("@supplier_id", supplierId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Supplier
                    {
                        supplier_id = reader.GetInt32(0),
                        supplier_name = reader.GetString(1),
                        contact_number = reader.IsDBNull(2) ? null : reader.GetString(2),
                        email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        is_active = reader.FieldCount > 4 && !reader.IsDBNull(4) ? reader.GetBoolean(4) : true,
                        created_date = reader.FieldCount > 5 && !reader.IsDBNull(5) ? reader.GetDateTime(5) : DateTime.Now,
                        modified_date = reader.FieldCount > 6 && !reader.IsDBNull(6) ? (DateTime?)reader.GetDateTime(6) : null
                    };
                }

                return new Supplier(); // Return empty supplier instead of null
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading supplier: {ex.Message}");
            }
        }
    }
}

