using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;
using System.Security.Cryptography;
using System.Text;

namespace MauiApp2.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string username, string password);
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        string CurrentUser { get; }
        int CurrentUserId { get; }
        string CurrentUserName { get; }
        int CurrentUserRoleId { get; }
        string CurrentUserRoleName { get; }
    }

    public class AuthService : IAuthService
    {
        private readonly IAuditLogService? _auditLogService;

        public AuthService(IAuditLogService? auditLogService = null)
        {
            _auditLogService = auditLogService;
        }

        public bool IsAuthenticated { get; private set; }
        public string CurrentUser { get; private set; } = string.Empty;
        public int CurrentUserId { get; private set; }
        public string CurrentUserName { get; private set; } = string.Empty;
        public int CurrentUserRoleId { get; private set; }
        public string CurrentUserRoleName { get; private set; } = string.Empty;

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return false;
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Try to find user by username or email with role name
                var command = new SqlCommand(@"
                    SELECT u.user_id, u.username, u.email, u.password_hash, u.full_name, u.is_active, u.role_id,
                           r.role_name
                    FROM tbl_users u
                    LEFT JOIN tbl_roles r ON u.role_id = r.role_id
                    WHERE (u.username = @username OR u.email = @username) 
                    AND u.is_active = 1", connection);

                command.Parameters.AddWithValue("@username", username.Trim());

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    int userId = reader.GetInt32(0);
                    string dbUsername = reader.GetString(1);
                    string dbEmail = reader.GetString(2);
                    string passwordHash = reader.GetString(3);
                    string fullName = reader.GetString(4);
                    bool isActive = reader.GetBoolean(5);
                    int roleId = reader.GetInt32(6);
                    string roleName = reader.IsDBNull(7) ? "User" : reader.GetString(7);

                    // Verify password
                    if (VerifyPassword(password, passwordHash))
                    {
                        // Update last_login timestamp
                        await UpdateLastLoginAsync(userId);
                        
                        IsAuthenticated = true;
                        CurrentUser = username.Trim();
                        CurrentUserId = userId;
                        CurrentUserName = fullName;
                        CurrentUserRoleId = roleId;
                        CurrentUserRoleName = roleName;
                        
                        // Log successful login
                        if (_auditLogService != null)
                        {
                            await _auditLogService.LogActionAsync(userId, "Login", null, null, null, 
                                new { username = dbUsername, email = dbEmail, role = roleName }, null, null, $"logged in: {dbUsername}");
                        }
                        
                        return true;
                    }
                    else
                    {
                        // Log failed login attempt (if we can identify the user)
                        if (_auditLogService != null && userId > 0)
                        {
                            await _auditLogService.LogActionAsync(userId, "Login Failed", null, null, null, 
                                new { username = dbUsername, reason = "Invalid password" }, null, null, $"login failed: {dbUsername}");
                        }
                    }
                }

                return false;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_users'"))
                {
                    Console.WriteLine("tbl_users table doesn't exist yet.");
                    return false;
                }
                Console.WriteLine($"Database error during login: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            int userId = CurrentUserId;
            string username = CurrentUser;
            
            IsAuthenticated = false;
            CurrentUser = string.Empty;
            CurrentUserId = 0;
            CurrentUserName = string.Empty;
            CurrentUserRoleId = 0;
            CurrentUserRoleName = string.Empty;
            
            // Log logout
            if (_auditLogService != null && userId > 0)
            {
                await _auditLogService.LogActionAsync(userId, "Logout", null, null, null, 
                    new { username = username }, null, null, $"logged out: {username}");
            }
        }

        // Hash password using SHA256
        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Verify password
        private bool VerifyPassword(string password, string hash)
        {
            string hashOfInput = HashPassword(password);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }

        // Update last login timestamp
        private async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var updateCommand = new SqlCommand(@"
                    UPDATE tbl_users 
                    SET last_login = @last_login 
                    WHERE user_id = @user_id", connection);

                updateCommand.Parameters.AddWithValue("@user_id", userId);
                updateCommand.Parameters.AddWithValue("@last_login", DateTime.Now);

                await updateCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't fail the login if last_login update fails
                Console.WriteLine($"Error updating last_login: {ex.Message}");
            }
        }
    }
}