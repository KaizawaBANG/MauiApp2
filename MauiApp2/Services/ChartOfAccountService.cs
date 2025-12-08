using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IChartOfAccountService
    {
        Task<List<ChartOfAccount>> GetAccountsAsync();
        Task<List<ChartOfAccount>> GetAccountsByTypeAsync(string accountType);
        Task<ChartOfAccount> GetAccountByIdAsync(int accountId);
        Task<ChartOfAccount> GetAccountByCodeAsync(string accountCode);
        Task<int> CreateAccountAsync(ChartOfAccount account);
        Task<bool> UpdateAccountAsync(ChartOfAccount account);
        Task<bool> DeleteAccountAsync(int accountId);
    }

    public class ChartOfAccountService : IChartOfAccountService
    {
        public async Task<List<ChartOfAccount>> GetAccountsAsync()
        {
            var accounts = new List<ChartOfAccount>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT account_id, account_code, account_name, account_type, 
                           description, is_active, created_date
                    FROM tbl_chart_of_accounts
                    ORDER BY account_type, account_code", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    accounts.Add(new ChartOfAccount
                    {
                        account_id = reader.GetInt32(0),
                        account_code = reader.GetString(1),
                        account_name = reader.GetString(2),
                        account_type = reader.GetString(3),
                        description = reader.IsDBNull(4) ? null : reader.GetString(4),
                        is_active = reader.GetBoolean(5),
                        created_date = reader.GetDateTime(6)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_chart_of_accounts'"))
                {
                    Console.WriteLine("tbl_chart_of_accounts table doesn't exist yet.");
                    return accounts;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading accounts: {ex.Message}");
            }

            return accounts;
        }

        public async Task<List<ChartOfAccount>> GetAccountsByTypeAsync(string accountType)
        {
            var accounts = new List<ChartOfAccount>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT account_id, account_code, account_name, account_type, 
                           description, is_active, created_date
                    FROM tbl_chart_of_accounts
                    WHERE account_type = @account_type AND is_active = 1
                    ORDER BY account_code", connection);
                command.Parameters.AddWithValue("@account_type", accountType);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    accounts.Add(new ChartOfAccount
                    {
                        account_id = reader.GetInt32(0),
                        account_code = reader.GetString(1),
                        account_name = reader.GetString(2),
                        account_type = reader.GetString(3),
                        description = reader.IsDBNull(4) ? null : reader.GetString(4),
                        is_active = reader.GetBoolean(5),
                        created_date = reader.GetDateTime(6)
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading accounts by type: {ex.Message}");
            }

            return accounts;
        }

        public async Task<ChartOfAccount> GetAccountByIdAsync(int accountId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT account_id, account_code, account_name, account_type, 
                           description, is_active, created_date
                    FROM tbl_chart_of_accounts
                    WHERE account_id = @account_id", connection);
                command.Parameters.AddWithValue("@account_id", accountId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new ChartOfAccount
                    {
                        account_id = reader.GetInt32(0),
                        account_code = reader.GetString(1),
                        account_name = reader.GetString(2),
                        account_type = reader.GetString(3),
                        description = reader.IsDBNull(4) ? null : reader.GetString(4),
                        is_active = reader.GetBoolean(5),
                        created_date = reader.GetDateTime(6)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading account: {ex.Message}");
            }

            throw new Exception("Account not found");
        }

        public async Task<ChartOfAccount> GetAccountByCodeAsync(string accountCode)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT account_id, account_code, account_name, account_type, 
                           description, is_active, created_date
                    FROM tbl_chart_of_accounts
                    WHERE account_code = @account_code", connection);
                command.Parameters.AddWithValue("@account_code", accountCode);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new ChartOfAccount
                    {
                        account_id = reader.GetInt32(0),
                        account_code = reader.GetString(1),
                        account_name = reader.GetString(2),
                        account_type = reader.GetString(3),
                        description = reader.IsDBNull(4) ? null : reader.GetString(4),
                        is_active = reader.GetBoolean(5),
                        created_date = reader.GetDateTime(6)
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading account: {ex.Message}");
            }

            throw new Exception("Account not found");
        }

        public async Task<int> CreateAccountAsync(ChartOfAccount account)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    INSERT INTO tbl_chart_of_accounts 
                    (account_code, account_name, account_type, description, is_active, created_date)
                    VALUES 
                    (@account_code, @account_name, @account_type, @description, @is_active, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);

                command.Parameters.AddWithValue("@account_code", account.account_code);
                command.Parameters.AddWithValue("@account_name", account.account_name);
                command.Parameters.AddWithValue("@account_type", account.account_type);
                command.Parameters.AddWithValue("@description", (object?)account.description ?? DBNull.Value);
                command.Parameters.AddWithValue("@is_active", account.is_active);

                var accountId = await command.ExecuteScalarAsync();
                return Convert.ToInt32(accountId);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating account: {ex.Message}");
            }
        }

        public async Task<bool> UpdateAccountAsync(ChartOfAccount account)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_chart_of_accounts
                    SET account_code = @account_code,
                        account_name = @account_name,
                        account_type = @account_type,
                        description = @description,
                        is_active = @is_active
                    WHERE account_id = @account_id", connection);

                command.Parameters.AddWithValue("@account_id", account.account_id);
                command.Parameters.AddWithValue("@account_code", account.account_code);
                command.Parameters.AddWithValue("@account_name", account.account_name);
                command.Parameters.AddWithValue("@account_type", account.account_type);
                command.Parameters.AddWithValue("@description", (object?)account.description ?? DBNull.Value);
                command.Parameters.AddWithValue("@is_active", account.is_active);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating account: {ex.Message}");
            }
        }

        public async Task<bool> DeleteAccountAsync(int accountId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Soft delete - just deactivate
                var command = new SqlCommand(@"
                    UPDATE tbl_chart_of_accounts
                    SET is_active = 0
                    WHERE account_id = @account_id", connection);

                command.Parameters.AddWithValue("@account_id", accountId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting account: {ex.Message}");
            }
        }
    }
}




