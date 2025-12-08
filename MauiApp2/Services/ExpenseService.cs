using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IExpenseService
    {
        Task<int> CreateExpenseAsync(Expense expense);
        Task<List<Expense>> GetExpensesAsync(DateTime? startDate = null, DateTime? endDate = null, string? category = null);
        Task<Expense> GetExpenseByIdAsync(int expenseId);
        Task<bool> UpdateExpenseAsync(Expense expense);
        Task<bool> DeleteExpenseAsync(int expenseId);
        Task<decimal> GetTotalExpensesByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class ExpenseService : IExpenseService
    {
        public async Task<int> CreateExpenseAsync(Expense expense)
        {
            using var connection = db.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Create expense record
                var command = new SqlCommand(@"
                    INSERT INTO tbl_expenses 
                    (expense_date, category, description, amount, payment_method, reference_number, created_by, created_date)
                    VALUES 
                    (@expense_date, @category, @description, @amount, @payment_method, @reference_number, @created_by, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction);

                command.Parameters.AddWithValue("@expense_date", expense.expense_date);
                command.Parameters.AddWithValue("@category", expense.category);
                command.Parameters.AddWithValue("@description", expense.description);
                command.Parameters.AddWithValue("@amount", expense.amount);
                command.Parameters.AddWithValue("@payment_method", expense.payment_method);
                command.Parameters.AddWithValue("@reference_number", (object?)expense.reference_number ?? DBNull.Value);
                command.Parameters.AddWithValue("@created_by", expense.created_by);

                var expenseId = Convert.ToInt32(await command.ExecuteScalarAsync());

                // Step 2: Create automatic ledger entries
                await CreateExpenseLedgerEntriesAsync(connection, transaction, expenseId, expense.category, expense.description, expense.amount, expense.created_by);

                transaction.Commit();
                return expenseId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error creating expense: {ex.Message}");
            }
        }

        // Create automatic ledger entries for expense
        private async Task CreateExpenseLedgerEntriesAsync(SqlConnection connection, SqlTransaction transaction, int expenseId, string category, string description, decimal amount, int createdBy)
        {
            try
            {
                // Get account IDs
                int cashAccountId = await GetAccountIdByCodeAsync(connection, transaction, "1001"); // Cash
                int expenseAccountId = await GetExpenseAccountIdByCategoryAsync(connection, transaction, category);

                // If accounts don't exist, skip accounting
                if (cashAccountId == 0 || expenseAccountId == 0)
                {
                    Console.WriteLine("Warning: Chart of Accounts not set up. Skipping automatic ledger entries.");
                    return;
                }

                // Expense Debit (increase expense)
                await CreateLedgerEntryAsync(connection, transaction, expenseAccountId, amount, 0,
                    $"{category}: {description}", "Expense", expenseId, createdBy);

                // Cash Credit (reduce cash)
                await CreateLedgerEntryAsync(connection, transaction, cashAccountId, 0, amount,
                    $"{category}: {description}", "Expense", expenseId, createdBy);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the expense
                Console.WriteLine($"Error creating ledger entries: {ex.Message}");
            }
        }

        // Helper: Get expense account ID by category
        private async Task<int> GetExpenseAccountIdByCategoryAsync(SqlConnection connection, SqlTransaction transaction, string category)
        {
            try
            {
                // Map category to account code
                string accountCode = category switch
                {
                    "Rent" => "5002",
                    "Utilities" => "5003",
                    "Salaries" => "5004",
                    "Supplies" => "5005",
                    "Marketing" => "5006",
                    _ => "5007" // Other Expenses
                };

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

        public async Task<List<Expense>> GetExpensesAsync(DateTime? startDate = null, DateTime? endDate = null, string? category = null)
        {
            var expenses = new List<Expense>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT e.expense_id, e.expense_date, e.category, e.description, e.amount,
                           e.payment_method, e.reference_number, e.created_by, e.created_date, e.modified_date,
                           u.full_name
                    FROM tbl_expenses e
                    LEFT JOIN tbl_users u ON e.created_by = u.user_id
                    WHERE 1=1";

                if (startDate.HasValue)
                {
                    query += " AND e.expense_date >= @start_date";
                }
                if (endDate.HasValue)
                {
                    query += " AND e.expense_date <= @end_date";
                }
                if (!string.IsNullOrEmpty(category))
                {
                    query += " AND e.category = @category";
                }

                query += " ORDER BY e.expense_date DESC, e.expense_id DESC";

                var command = new SqlCommand(query, connection);

                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@start_date", startDate.Value);
                }
                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@end_date", endDate.Value);
                }
                if (!string.IsNullOrEmpty(category))
                {
                    command.Parameters.AddWithValue("@category", category);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    expenses.Add(new Expense
                    {
                        expense_id = reader.GetInt32(0),
                        expense_date = reader.GetDateTime(1),
                        category = reader.GetString(2),
                        description = reader.GetString(3),
                        amount = reader.GetDecimal(4),
                        payment_method = reader.GetString(5),
                        reference_number = reader.IsDBNull(6) ? null : reader.GetString(6),
                        created_by = reader.GetInt32(7),
                        created_date = reader.GetDateTime(8),
                        modified_date = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                        created_by_name = reader.IsDBNull(10) ? null : reader.GetString(10)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading expenses: {ex.Message}");
            }

            return expenses;
        }

        public async Task<Expense> GetExpenseByIdAsync(int expenseId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT e.expense_id, e.expense_date, e.category, e.description, e.amount,
                           e.payment_method, e.reference_number, e.created_by, e.created_date, e.modified_date,
                           u.full_name
                    FROM tbl_expenses e
                    LEFT JOIN tbl_users u ON e.created_by = u.user_id
                    WHERE e.expense_id = @expense_id", connection);

                command.Parameters.AddWithValue("@expense_id", expenseId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Expense
                    {
                        expense_id = reader.GetInt32(0),
                        expense_date = reader.GetDateTime(1),
                        category = reader.GetString(2),
                        description = reader.GetString(3),
                        amount = reader.GetDecimal(4),
                        payment_method = reader.GetString(5),
                        reference_number = reader.IsDBNull(6) ? null : reader.GetString(6),
                        created_by = reader.GetInt32(7),
                        created_date = reader.GetDateTime(8),
                        modified_date = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                        created_by_name = reader.IsDBNull(10) ? null : reader.GetString(10)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading expense: {ex.Message}");
            }

            throw new Exception("Expense not found");
        }

        public async Task<bool> UpdateExpenseAsync(Expense expense)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_expenses
                    SET expense_date = @expense_date,
                        category = @category,
                        description = @description,
                        amount = @amount,
                        payment_method = @payment_method,
                        reference_number = @reference_number,
                        modified_date = GETDATE()
                    WHERE expense_id = @expense_id", connection);

                command.Parameters.AddWithValue("@expense_id", expense.expense_id);
                command.Parameters.AddWithValue("@expense_date", expense.expense_date);
                command.Parameters.AddWithValue("@category", expense.category);
                command.Parameters.AddWithValue("@description", expense.description);
                command.Parameters.AddWithValue("@amount", expense.amount);
                command.Parameters.AddWithValue("@payment_method", expense.payment_method);
                command.Parameters.AddWithValue("@reference_number", (object?)expense.reference_number ?? DBNull.Value);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating expense: {ex.Message}");
            }
        }

        public async Task<bool> DeleteExpenseAsync(int expenseId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    DELETE FROM tbl_expenses
                    WHERE expense_id = @expense_id", connection);

                command.Parameters.AddWithValue("@expense_id", expenseId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting expense: {ex.Message}");
            }
        }

        public async Task<decimal> GetTotalExpensesByCategoryAsync(string category, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ISNULL(SUM(amount), 0)
                    FROM tbl_expenses
                    WHERE category = @category";

                if (startDate.HasValue)
                {
                    query += " AND expense_date >= @start_date";
                }
                if (endDate.HasValue)
                {
                    query += " AND expense_date <= @end_date";
                }

                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@category", category);

                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@start_date", startDate.Value);
                }
                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@end_date", endDate.Value);
                }

                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating total expenses: {ex.Message}");
            }
        }
    }
}

