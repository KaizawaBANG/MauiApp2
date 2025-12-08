using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IPaymentService
    {
        Task<int> CreatePaymentAsync(Payment payment);
        Task<List<Payment>> GetPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null, int? apId = null);
        Task<Payment> GetPaymentByIdAsync(int paymentId);
        Task<List<Payment>> GetPaymentsByApIdAsync(int apId);
    }

    public class PaymentService : IPaymentService
    {
        public async Task<int> CreatePaymentAsync(Payment payment)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Create payment record
                var command = new SqlCommand(@"
                    INSERT INTO tbl_payments 
                    (ap_id, payment_date, amount, payment_method, reference_number, notes, created_by, created_date)
                    VALUES 
                    (@ap_id, @payment_date, @amount, @payment_method, @reference_number, @notes, @created_by, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction);

                command.Parameters.AddWithValue("@ap_id", (object?)payment.ap_id ?? DBNull.Value);
                command.Parameters.AddWithValue("@payment_date", payment.payment_date);
                command.Parameters.AddWithValue("@amount", payment.amount);
                command.Parameters.AddWithValue("@payment_method", payment.payment_method);
                command.Parameters.AddWithValue("@reference_number", (object?)payment.reference_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@notes", (object?)payment.notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@created_by", payment.created_by);

                var paymentId = Convert.ToInt32(await command.ExecuteScalarAsync());

                // Step 2: Update Accounts Payable if payment is linked to AP
                if (payment.ap_id.HasValue)
                {
                    await UpdateAccountsPayableAsync(connection, transaction, payment.ap_id.Value, payment.amount);
                }

                // Step 3: Create automatic ledger entries
                await CreatePaymentLedgerEntriesAsync(connection, transaction, paymentId, payment.amount, payment.ap_id, payment.created_by);

                transaction.Commit();
                return paymentId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error creating payment: {ex.Message}");
            }
        }

        // Update Accounts Payable when payment is made
        private async Task UpdateAccountsPayableAsync(SqlConnection connection, SqlTransaction transaction, int apId, decimal paymentAmount)
        {
            // Get current AP record
            var getCommand = new SqlCommand(@"
                SELECT total_amount, paid_amount FROM tbl_accounts_payable WHERE ap_id = @ap_id", connection, transaction);
            getCommand.Parameters.AddWithValue("@ap_id", apId);

            using var reader = await getCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                decimal totalAmount = reader.GetDecimal(0);
                decimal currentPaid = reader.GetDecimal(1);
                decimal newPaidAmount = currentPaid + paymentAmount;

                string newStatus;
                if (newPaidAmount >= totalAmount)
                {
                    newStatus = "Paid";
                    newPaidAmount = totalAmount; // Don't overpay
                }
                else if (newPaidAmount > 0)
                {
                    newStatus = "Partial";
                }
                else
                {
                    newStatus = "Unpaid";
                }

                reader.Close();

                // Update AP record
                var updateCommand = new SqlCommand(@"
                    UPDATE tbl_accounts_payable
                    SET paid_amount = @paid_amount,
                        status = @status,
                        modified_date = GETDATE()
                    WHERE ap_id = @ap_id", connection, transaction);

                updateCommand.Parameters.AddWithValue("@ap_id", apId);
                updateCommand.Parameters.AddWithValue("@paid_amount", newPaidAmount);
                updateCommand.Parameters.AddWithValue("@status", newStatus);

                await updateCommand.ExecuteNonQueryAsync();
            }
        }

        // Create automatic ledger entries for payment
        private async Task CreatePaymentLedgerEntriesAsync(SqlConnection connection, SqlTransaction transaction, int paymentId, decimal amount, int? apId, int createdBy)
        {
            try
            {
                // Get account IDs
                int cashAccountId = await GetAccountIdByCodeAsync(connection, transaction, "1001"); // Cash
                int apAccountId = await GetAccountIdByCodeAsync(connection, transaction, "2001"); // Accounts Payable

                // If accounts don't exist, skip accounting
                if (cashAccountId == 0 || apAccountId == 0)
                {
                    Console.WriteLine("Warning: Chart of Accounts not set up. Skipping automatic ledger entries.");
                    return;
                }

                string description = apId.HasValue 
                    ? $"Payment to Supplier - Payment #{paymentId}" 
                    : $"Payment - Payment #{paymentId}";

                // Accounts Payable Debit (reduce debt)
                if (apId.HasValue)
                {
                    await CreateLedgerEntryAsync(connection, transaction, apAccountId, amount, 0,
                        description, "Payment", paymentId, createdBy);
                }

                // Cash Credit (reduce cash)
                await CreateLedgerEntryAsync(connection, transaction, cashAccountId, 0, amount,
                    description, "Payment", paymentId, createdBy);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the payment
                Console.WriteLine($"Error creating ledger entries: {ex.Message}");
            }
        }

        // Helper: Get account ID by code
        private async Task<int> GetAccountIdByCodeAsync(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            try
            {
                var command = new SqlCommand(@"
                    SELECT account_id FROM tbl_chart_of_accounts 
                    WHERE account_code = @account_code AND is_active = 1", connection, transaction);
                command.Parameters.AddWithValue("@account_code", accountCode);

                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        // Helper: Create ledger entry within transaction
        private async Task CreateLedgerEntryAsync(SqlConnection connection, SqlTransaction transaction, int accountId,
            decimal debitAmount, decimal creditAmount, string description, string referenceType, int referenceId, int createdBy)
        {
            var command = new SqlCommand(@"
                INSERT INTO tbl_general_ledger 
                (transaction_date, account_id, debit_amount, credit_amount, description, 
                 reference_type, reference_id, created_by, created_date)
                VALUES 
                (GETDATE(), @account_id, @debit_amount, @credit_amount, @description,
                 @reference_type, @reference_id, @created_by, GETDATE())", connection, transaction);

            command.Parameters.AddWithValue("@account_id", accountId);
            command.Parameters.AddWithValue("@debit_amount", debitAmount);
            command.Parameters.AddWithValue("@credit_amount", creditAmount);
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@reference_type", referenceType);
            command.Parameters.AddWithValue("@reference_id", referenceId);
            command.Parameters.AddWithValue("@created_by", createdBy);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Payment>> GetPaymentsAsync(DateTime? startDate = null, DateTime? endDate = null, int? apId = null)
        {
            var payments = new List<Payment>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT p.payment_id, p.ap_id, p.payment_date, p.amount, p.payment_method,
                           p.reference_number, p.notes, p.created_by, p.created_date,
                           u.full_name,
                           s.supplier_name
                    FROM tbl_payments p
                    LEFT JOIN tbl_users u ON p.created_by = u.user_id
                    LEFT JOIN tbl_accounts_payable ap ON p.ap_id = ap.ap_id
                    LEFT JOIN tbl_supplier s ON ap.supplier_id = s.supplier_id
                    WHERE 1=1";

                if (startDate.HasValue)
                {
                    query += " AND p.payment_date >= @start_date";
                }
                if (endDate.HasValue)
                {
                    query += " AND p.payment_date <= @end_date";
                }
                if (apId.HasValue)
                {
                    query += " AND p.ap_id = @ap_id";
                }

                query += " ORDER BY p.payment_date DESC, p.payment_id DESC";

                var command = new SqlCommand(query, connection);

                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@start_date", startDate.Value);
                }
                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@end_date", endDate.Value);
                }
                if (apId.HasValue)
                {
                    command.Parameters.AddWithValue("@ap_id", apId.Value);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    payments.Add(new Payment
                    {
                        payment_id = reader.GetInt32(0),
                        ap_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        payment_date = reader.GetDateTime(2),
                        amount = reader.GetDecimal(3),
                        payment_method = reader.GetString(4),
                        reference_number = reader.IsDBNull(5) ? null : reader.GetString(5),
                        notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                        created_by = reader.GetInt32(7),
                        created_date = reader.GetDateTime(8),
                        created_by_name = reader.IsDBNull(9) ? null : reader.GetString(9),
                        supplier_name = reader.IsDBNull(10) ? null : reader.GetString(10)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading payments: {ex.Message}");
            }

            return payments;
        }

        public async Task<Payment> GetPaymentByIdAsync(int paymentId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT p.payment_id, p.ap_id, p.payment_date, p.amount, p.payment_method,
                           p.reference_number, p.notes, p.created_by, p.created_date,
                           u.full_name,
                           s.supplier_name
                    FROM tbl_payments p
                    LEFT JOIN tbl_users u ON p.created_by = u.user_id
                    LEFT JOIN tbl_accounts_payable ap ON p.ap_id = ap.ap_id
                    LEFT JOIN tbl_supplier s ON ap.supplier_id = s.supplier_id
                    WHERE p.payment_id = @payment_id", connection);

                command.Parameters.AddWithValue("@payment_id", paymentId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Payment
                    {
                        payment_id = reader.GetInt32(0),
                        ap_id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        payment_date = reader.GetDateTime(2),
                        amount = reader.GetDecimal(3),
                        payment_method = reader.GetString(4),
                        reference_number = reader.IsDBNull(5) ? null : reader.GetString(5),
                        notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                        created_by = reader.GetInt32(7),
                        created_date = reader.GetDateTime(8),
                        created_by_name = reader.IsDBNull(9) ? null : reader.GetString(9),
                        supplier_name = reader.IsDBNull(10) ? null : reader.GetString(10)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading payment: {ex.Message}");
            }

            throw new Exception("Payment not found");
        }

        public async Task<List<Payment>> GetPaymentsByApIdAsync(int apId)
        {
            return await GetPaymentsAsync(null, null, apId);
        }
    }
}

