using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IGeneralLedgerService
    {
        Task<int> CreateLedgerEntryAsync(int accountId, decimal debitAmount, decimal creditAmount, 
            string description, string? referenceType, int? referenceId, int createdBy, DateTime? transactionDate = null);
        Task<List<GeneralLedger>> GetLedgerEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, int? accountId = null);
        Task<List<GeneralLedger>> GetLedgerEntriesByAccountAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null);
        Task<List<GeneralLedger>> GetLedgerEntriesByReferenceAsync(string referenceType, int referenceId);
    }

    public class GeneralLedgerService : IGeneralLedgerService
    {
        public async Task<int> CreateLedgerEntryAsync(int accountId, decimal debitAmount, decimal creditAmount,
            string description, string? referenceType, int? referenceId, int createdBy, DateTime? transactionDate = null)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    INSERT INTO tbl_general_ledger 
                    (transaction_date, account_id, debit_amount, credit_amount, description, 
                     reference_type, reference_id, created_by, created_date)
                    VALUES 
                    (@transaction_date, @account_id, @debit_amount, @credit_amount, @description,
                     @reference_type, @reference_id, @created_by, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);

                command.Parameters.AddWithValue("@transaction_date", transactionDate ?? DateTime.Now);
                command.Parameters.AddWithValue("@account_id", accountId);
                command.Parameters.AddWithValue("@debit_amount", debitAmount);
                command.Parameters.AddWithValue("@credit_amount", creditAmount);
                command.Parameters.AddWithValue("@description", description);
                command.Parameters.AddWithValue("@reference_type", (object?)referenceType ?? DBNull.Value);
                command.Parameters.AddWithValue("@reference_id", (object?)referenceId ?? DBNull.Value);
                command.Parameters.AddWithValue("@created_by", createdBy);

                var ledgerId = await command.ExecuteScalarAsync();
                return Convert.ToInt32(ledgerId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating ledger entry: {ex.Message}");
            }
        }

        public async Task<List<GeneralLedger>> GetLedgerEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, int? accountId = null)
        {
            var entries = new List<GeneralLedger>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT gl.ledger_id, gl.transaction_date, gl.account_id, gl.debit_amount, 
                           gl.credit_amount, gl.description, gl.reference_type, gl.reference_id,
                           gl.created_by, gl.created_date,
                           coa.account_name, coa.account_code, coa.account_type,
                           u.full_name
                    FROM tbl_general_ledger gl
                    INNER JOIN tbl_chart_of_accounts coa ON gl.account_id = coa.account_id
                    LEFT JOIN tbl_users u ON gl.created_by = u.user_id
                    WHERE 1=1";

                if (startDate.HasValue)
                {
                    query += " AND gl.transaction_date >= @start_date";
                }
                if (endDate.HasValue)
                {
                    query += " AND gl.transaction_date <= @end_date";
                }
                if (accountId.HasValue)
                {
                    query += " AND gl.account_id = @account_id";
                }

                query += " ORDER BY gl.transaction_date DESC, gl.ledger_id DESC";

                var command = new SqlCommand(query, connection);

                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@start_date", startDate.Value);
                }
                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@end_date", endDate.Value);
                }
                if (accountId.HasValue)
                {
                    command.Parameters.AddWithValue("@account_id", accountId.Value);
                }

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    entries.Add(new GeneralLedger
                    {
                        ledger_id = reader.GetInt32(0),
                        transaction_date = reader.GetDateTime(1),
                        account_id = reader.GetInt32(2),
                        debit_amount = reader.GetDecimal(3),
                        credit_amount = reader.GetDecimal(4),
                        description = reader.GetString(5),
                        reference_type = reader.IsDBNull(6) ? null : reader.GetString(6),
                        reference_id = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                        created_by = reader.GetInt32(8),
                        created_date = reader.GetDateTime(9),
                        account_name = reader.IsDBNull(10) ? null : reader.GetString(10),
                        account_code = reader.IsDBNull(11) ? null : reader.GetString(11),
                        account_type = reader.IsDBNull(12) ? null : reader.GetString(12),
                        created_by_name = reader.IsDBNull(13) ? null : reader.GetString(13)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading ledger entries: {ex.Message}");
            }

            return entries;
        }

        public async Task<List<GeneralLedger>> GetLedgerEntriesByAccountAsync(int accountId, DateTime? startDate = null, DateTime? endDate = null)
        {
            return await GetLedgerEntriesAsync(startDate, endDate, accountId);
        }

        public async Task<decimal> GetAccountBalanceAsync(int accountId, DateTime? asOfDate = null)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        ISNULL(SUM(debit_amount), 0) - ISNULL(SUM(credit_amount), 0) AS balance
                    FROM tbl_general_ledger
                    WHERE account_id = @account_id";

                if (asOfDate.HasValue)
                {
                    query += " AND transaction_date <= @as_of_date";
                }

                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@account_id", accountId);

                if (asOfDate.HasValue)
                {
                    command.Parameters.AddWithValue("@as_of_date", asOfDate.Value);
                }

                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating account balance: {ex.Message}");
            }
        }

        public async Task<List<GeneralLedger>> GetLedgerEntriesByReferenceAsync(string referenceType, int referenceId)
        {
            var entries = new List<GeneralLedger>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT gl.ledger_id, gl.transaction_date, gl.account_id, gl.debit_amount, 
                           gl.credit_amount, gl.description, gl.reference_type, gl.reference_id,
                           gl.created_by, gl.created_date,
                           coa.account_name, coa.account_code, coa.account_type,
                           u.full_name
                    FROM tbl_general_ledger gl
                    INNER JOIN tbl_chart_of_accounts coa ON gl.account_id = coa.account_id
                    LEFT JOIN tbl_users u ON gl.created_by = u.user_id
                    WHERE gl.reference_type = @reference_type AND gl.reference_id = @reference_id
                    ORDER BY gl.transaction_date DESC", connection);

                command.Parameters.AddWithValue("@reference_type", referenceType);
                command.Parameters.AddWithValue("@reference_id", referenceId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    entries.Add(new GeneralLedger
                    {
                        ledger_id = reader.GetInt32(0),
                        transaction_date = reader.GetDateTime(1),
                        account_id = reader.GetInt32(2),
                        debit_amount = reader.GetDecimal(3),
                        credit_amount = reader.GetDecimal(4),
                        description = reader.GetString(5),
                        reference_type = reader.IsDBNull(6) ? null : reader.GetString(6),
                        reference_id = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                        created_by = reader.GetInt32(8),
                        created_date = reader.GetDateTime(9),
                        account_name = reader.IsDBNull(10) ? null : reader.GetString(10),
                        account_code = reader.IsDBNull(11) ? null : reader.GetString(11),
                        account_type = reader.IsDBNull(12) ? null : reader.GetString(12),
                        created_by_name = reader.IsDBNull(13) ? null : reader.GetString(13)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading ledger entries by reference: {ex.Message}");
            }

            return entries;
        }
    }
}

