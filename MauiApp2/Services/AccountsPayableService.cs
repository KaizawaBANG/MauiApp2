using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IAccountsPayableService
    {
        Task<List<AccountsPayable>> GetAccountsPayableAsync(string? status = null);
        Task<AccountsPayable> GetAccountsPayableByIdAsync(int apId);
        Task<AccountsPayable> GetAccountsPayableByPoIdAsync(int poId);
        Task<int> CreateAccountsPayableAsync(AccountsPayable ap);
        Task<bool> UpdateAccountsPayableAsync(AccountsPayable ap);
        Task<bool> UpdatePaidAmountAsync(int apId, decimal paidAmount);
    }

    public class AccountsPayableService : IAccountsPayableService
    {
        public async Task<List<AccountsPayable>> GetAccountsPayableAsync(string? status = null)
        {
            var accountsPayable = new List<AccountsPayable>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ap.ap_id, ap.po_id, ap.supplier_id, ap.invoice_number, 
                           ap.total_amount, ap.paid_amount, ap.balance_amount,
                           ap.due_date, ap.status, ap.created_date, ap.modified_date,
                           s.supplier_name,
                           po.po_number
                    FROM tbl_accounts_payable ap
                    INNER JOIN tbl_supplier s ON ap.supplier_id = s.supplier_id
                    LEFT JOIN tbl_purchase_order po ON ap.po_id = po.po_id
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ap.status = @status";
                }

                query += " ORDER BY ap.created_date DESC";

                var command = new SqlCommand(query, connection);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@status", status);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    accountsPayable.Add(new AccountsPayable
                    {
                        ap_id = reader.GetInt32(0),
                        po_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        supplier_id = reader.GetInt32(2),
                        invoice_number = reader.IsDBNull(3) ? null : reader.GetString(3),
                        total_amount = reader.GetDecimal(4),
                        paid_amount = reader.GetDecimal(5),
                        balance_amount = reader.GetDecimal(6),
                        due_date = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                        status = reader.GetString(8),
                        created_date = reader.GetDateTime(9),
                        modified_date = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                        supplier_name = reader.IsDBNull(11) ? null : reader.GetString(11),
                        po_number = reader.IsDBNull(12) ? null : reader.GetString(12)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading accounts payable: {ex.Message}");
            }

            return accountsPayable;
        }

        public async Task<AccountsPayable> GetAccountsPayableByIdAsync(int apId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT ap.ap_id, ap.po_id, ap.supplier_id, ap.invoice_number, 
                           ap.total_amount, ap.paid_amount, ap.balance_amount,
                           ap.due_date, ap.status, ap.created_date, ap.modified_date,
                           s.supplier_name,
                           po.po_number
                    FROM tbl_accounts_payable ap
                    INNER JOIN tbl_supplier s ON ap.supplier_id = s.supplier_id
                    LEFT JOIN tbl_purchase_order po ON ap.po_id = po.po_id
                    WHERE ap.ap_id = @ap_id", connection);

                command.Parameters.AddWithValue("@ap_id", apId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new AccountsPayable
                    {
                        ap_id = reader.GetInt32(0),
                        po_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        supplier_id = reader.GetInt32(2),
                        invoice_number = reader.IsDBNull(3) ? null : reader.GetString(3),
                        total_amount = reader.GetDecimal(4),
                        paid_amount = reader.GetDecimal(5),
                        balance_amount = reader.GetDecimal(6),
                        due_date = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                        status = reader.GetString(8),
                        created_date = reader.GetDateTime(9),
                        modified_date = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                        supplier_name = reader.IsDBNull(11) ? null : reader.GetString(11),
                        po_number = reader.IsDBNull(12) ? null : reader.GetString(12)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading accounts payable: {ex.Message}");
            }

            throw new Exception("Accounts Payable not found");
        }

        public async Task<AccountsPayable> GetAccountsPayableByPoIdAsync(int poId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT ap.ap_id, ap.po_id, ap.supplier_id, ap.invoice_number, 
                           ap.total_amount, ap.paid_amount, ap.balance_amount,
                           ap.due_date, ap.status, ap.created_date, ap.modified_date,
                           s.supplier_name,
                           po.po_number
                    FROM tbl_accounts_payable ap
                    INNER JOIN tbl_supplier s ON ap.supplier_id = s.supplier_id
                    LEFT JOIN tbl_purchase_order po ON ap.po_id = po.po_id
                    WHERE ap.po_id = @po_id", connection);

                command.Parameters.AddWithValue("@po_id", poId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new AccountsPayable
                    {
                        ap_id = reader.GetInt32(0),
                        po_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        supplier_id = reader.GetInt32(2),
                        invoice_number = reader.IsDBNull(3) ? null : reader.GetString(3),
                        total_amount = reader.GetDecimal(4),
                        paid_amount = reader.GetDecimal(5),
                        balance_amount = reader.GetDecimal(6),
                        due_date = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                        status = reader.GetString(8),
                        created_date = reader.GetDateTime(9),
                        modified_date = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                        supplier_name = reader.IsDBNull(11) ? null : reader.GetString(11),
                        po_number = reader.IsDBNull(12) ? null : reader.GetString(12)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading accounts payable: {ex.Message}");
            }

            throw new Exception("Accounts Payable not found for this Purchase Order");
        }

        public async Task<int> CreateAccountsPayableAsync(AccountsPayable ap)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    INSERT INTO tbl_accounts_payable 
                    (po_id, supplier_id, invoice_number, total_amount, paid_amount, 
                     due_date, status, created_date)
                    VALUES 
                    (@po_id, @supplier_id, @invoice_number, @total_amount, @paid_amount,
                     @due_date, @status, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);

                command.Parameters.AddWithValue("@po_id", (object?)ap.po_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@supplier_id", ap.supplier_id);
                command.Parameters.AddWithValue("@invoice_number", (object?)ap.invoice_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@total_amount", ap.total_amount);
                command.Parameters.AddWithValue("@paid_amount", ap.paid_amount);
                command.Parameters.AddWithValue("@due_date", (object?)ap.due_date ?? DBNull.Value);
                command.Parameters.AddWithValue("@status", ap.status);

                var apId = await command.ExecuteScalarAsync();
                return Convert.ToInt32(apId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating accounts payable: {ex.Message}");
            }
        }

        public async Task<bool> UpdateAccountsPayableAsync(AccountsPayable ap)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_accounts_payable
                    SET po_id = @po_id,
                        supplier_id = @supplier_id,
                        invoice_number = @invoice_number,
                        total_amount = @total_amount,
                        paid_amount = @paid_amount,
                        due_date = @due_date,
                        status = @status,
                        modified_date = GETDATE()
                    WHERE ap_id = @ap_id", connection);

                command.Parameters.AddWithValue("@ap_id", ap.ap_id);
                command.Parameters.AddWithValue("@po_id", (object?)ap.po_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@supplier_id", ap.supplier_id);
                command.Parameters.AddWithValue("@invoice_number", (object?)ap.invoice_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@total_amount", ap.total_amount);
                command.Parameters.AddWithValue("@paid_amount", ap.paid_amount);
                command.Parameters.AddWithValue("@due_date", (object?)ap.due_date ?? DBNull.Value);
                command.Parameters.AddWithValue("@status", ap.status);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating accounts payable: {ex.Message}");
            }
        }

        public async Task<bool> UpdatePaidAmountAsync(int apId, decimal paidAmount)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Get current AP record
                var ap = await GetAccountsPayableByIdAsync(apId);
                
                var newPaidAmount = ap.paid_amount + paidAmount;
                string newStatus;

                if (newPaidAmount >= ap.total_amount)
                {
                    newStatus = "Paid";
                    newPaidAmount = ap.total_amount; // Ensure we don't overpay
                }
                else if (newPaidAmount > 0)
                {
                    newStatus = "Partial";
                }
                else
                {
                    newStatus = "Unpaid";
                }

                var command = new SqlCommand(@"
                    UPDATE tbl_accounts_payable
                    SET paid_amount = @paid_amount,
                        status = @status,
                        modified_date = GETDATE()
                    WHERE ap_id = @ap_id", connection);

                command.Parameters.AddWithValue("@ap_id", apId);
                command.Parameters.AddWithValue("@paid_amount", newPaidAmount);
                command.Parameters.AddWithValue("@status", newStatus);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating paid amount: {ex.Message}");
            }
        }
    }
}




