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
                return Convert.ToInt32(result);
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

                return await command.ExecuteNonQueryAsync() > 0;
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
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand("DELETE FROM tbl_supplier WHERE supplier_id = @supplier_id", connection);
                command.Parameters.AddWithValue("@supplier_id", supplierId);

                return await command.ExecuteNonQueryAsync() > 0;
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

