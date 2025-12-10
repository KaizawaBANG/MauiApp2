using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetCustomersAsync();
        Task<int> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(int customerId);
        Task<Customer> GetCustomerByIdAsync(int customerId);
        int GetWalkInCustomerId(); // Returns the walk-in customer ID (always 1)
    }

    public class CustomerService : ICustomerService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public CustomerService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // READ - Get all customers
        public async Task<List<Customer>> GetCustomersAsync()
        {
            var customers = new List<Customer>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT customer_id, customer_name, contact_number, email, address, is_active, created_date, modified_date
                    FROM tbl_customer
                    WHERE customer_id != 1
                    ORDER BY customer_name", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    customers.Add(new Customer
                    {
                        customer_id = reader.GetInt32(0),
                        customer_name = reader.GetString(1),
                        contact_number = reader.IsDBNull(2) ? null : reader.GetString(2),
                        email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        address = reader.IsDBNull(4) ? null : reader.GetString(4),
                        is_active = reader.FieldCount > 5 && !reader.IsDBNull(5) ? reader.GetBoolean(5) : true,
                        created_date = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetDateTime(6) : DateTime.Now,
                        modified_date = reader.FieldCount > 7 && !reader.IsDBNull(7) ? (DateTime?)reader.GetDateTime(7) : null
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_customer'"))
                {
                    Console.WriteLine("tbl_customer table doesn't exist yet.");
                    return customers;
                }
                if (ex.Message.Contains("Invalid column name"))
                {
                    Console.WriteLine("Customer table is missing required columns. Please run the CreateCustomerTables.sql script.");
                    throw new Exception("Customer table is missing required columns. Please run the CreateCustomerTables.sql script.");
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading customers: {ex.Message}");
            }

            return customers;
        }

        // CREATE - Add new customer
        public async Task<int> CreateCustomerAsync(Customer customer)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    INSERT INTO tbl_customer (customer_name, contact_number, email, address, is_active, created_date)
                    VALUES (@customer_name, @contact_number, @email, @address, @is_active, @created_date);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@customer_name", customer.customer_name);
                command.Parameters.AddWithValue("@contact_number", (object)customer.contact_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@email", (object)customer.email ?? DBNull.Value);
                command.Parameters.AddWithValue("@address", (object)customer.address ?? DBNull.Value);
                command.Parameters.AddWithValue("@is_active", customer.is_active);
                command.Parameters.AddWithValue("@created_date", customer.created_date);

                var result = await command.ExecuteScalarAsync();
                var customerId = Convert.ToInt32(result);

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_customer",
                        customerId,
                        null,
                        new { customer_name = customer.customer_name, contact_number = customer.contact_number, email = customer.email, address = customer.address, is_active = customer.is_active },
                        null,
                        null,
                        $"added new customer: {customer.customer_name}"
                    );
                }

                return customerId;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_customer'"))
                {
                    throw new Exception("tbl_customer table doesn't exist. Please create the table first.");
                }
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key") || ex.Message.Contains("UQ_customer_email"))
                {
                    throw new Exception($"Email already exists. Please use a different email.");
                }
                throw new Exception($"Error creating customer: {ex.Message}");
            }
        }

        // UPDATE - Modify existing customer
        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                // Get old values for audit log
                Customer? oldCustomer = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    oldCustomer = await GetCustomerByIdAsync(customer.customer_id);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_customer 
                    SET customer_name = @customer_name, 
                        contact_number = @contact_number,
                        email = @email,
                        address = @address,
                        is_active = @is_active,
                        modified_date = @modified_date
                    WHERE customer_id = @customer_id", connection);

                command.Parameters.AddWithValue("@customer_id", customer.customer_id);
                command.Parameters.AddWithValue("@customer_name", customer.customer_name);
                command.Parameters.AddWithValue("@contact_number", (object)customer.contact_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@email", (object)customer.email ?? DBNull.Value);
                command.Parameters.AddWithValue("@address", (object)customer.address ?? DBNull.Value);
                command.Parameters.AddWithValue("@is_active", customer.is_active);
                command.Parameters.AddWithValue("@modified_date", DateTime.Now);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldCustomer != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_customer",
                        customer.customer_id,
                        new { customer_name = oldCustomer.customer_name, contact_number = oldCustomer.contact_number, email = oldCustomer.email, address = oldCustomer.address, is_active = oldCustomer.is_active },
                        new { customer_name = customer.customer_name, contact_number = customer.contact_number, email = customer.email, address = customer.address, is_active = customer.is_active },
                        null,
                        null,
                        $"updated customer: {customer.customer_name}"
                    );
                }

                return success;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key") || ex.Message.Contains("UQ_customer_email"))
                {
                    throw new Exception($"Email already exists. Please use a different email.");
                }
                throw new Exception($"Error updating customer: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating customer: {ex.Message}");
            }
        }

        // DELETE - Remove customer
        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            try
            {
                // Get customer details for audit log before deletion
                Customer? customerToDelete = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    customerToDelete = await GetCustomerByIdAsync(customerId);
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand("DELETE FROM tbl_customer WHERE customer_id = @customer_id", connection);
                command.Parameters.AddWithValue("@customer_id", customerId);

                var success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && customerToDelete != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Delete",
                        "tbl_customer",
                        customerId,
                        new { customer_name = customerToDelete.customer_name, contact_number = customerToDelete.contact_number, email = customerToDelete.email, address = customerToDelete.address, is_active = customerToDelete.is_active },
                        null,
                        null,
                        null,
                        $"deleted customer: {customerToDelete.customer_name}"
                    );
                }

                return success;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("FOREIGN KEY constraint"))
                {
                    throw new Exception("Cannot delete customer. This customer is being used in sales orders.");
                }
                throw new Exception($"Error deleting customer: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting customer: {ex.Message}");
            }
        }

        // READ - Get customer by ID
        public async Task<Customer> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT customer_id, customer_name, contact_number, email, address, is_active, created_date, modified_date
                    FROM tbl_customer
                    WHERE customer_id = @customer_id", connection);

                command.Parameters.AddWithValue("@customer_id", customerId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Customer
                    {
                        customer_id = reader.GetInt32(0),
                        customer_name = reader.GetString(1),
                        contact_number = reader.IsDBNull(2) ? null : reader.GetString(2),
                        email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        address = reader.IsDBNull(4) ? null : reader.GetString(4),
                        is_active = reader.FieldCount > 5 && !reader.IsDBNull(5) ? reader.GetBoolean(5) : true,
                        created_date = reader.FieldCount > 6 && !reader.IsDBNull(6) ? reader.GetDateTime(6) : DateTime.Now,
                        modified_date = reader.FieldCount > 7 && !reader.IsDBNull(7) ? (DateTime?)reader.GetDateTime(7) : null
                    };
                }

                return new Customer(); // Return empty customer instead of null
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading customer: {ex.Message}");
            }
        }

        // Get Walk-in Customer ID (always 1)
        public int GetWalkInCustomerId()
        {
            return 1;
        }
    }
}

