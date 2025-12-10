using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IUserService
    {
        Task<List<User>> GetUsersAsync();
        Task<int> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> UpdateUserPasswordAsync(int userId, string newPassword);
        Task<bool> DeleteUserAsync(int userId);
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class UserService : IUserService
    {
        private readonly IAuditLogService? _auditLogService;
        private readonly IAuthService? _authService;

        public UserService(IAuditLogService? auditLogService = null, IAuthService? authService = null)
        {
            _auditLogService = auditLogService;
            _authService = authService;
        }

        // READ - Get all users with role names
        public async Task<List<User>> GetUsersAsync()
        {
            var users = new List<User>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT u.user_id, u.role_id, u.username, u.email, u.password_hash, 
                           u.full_name, u.is_active, u.last_login, u.created_date 
                    FROM tbl_users u 
                    ORDER BY u.created_date DESC, u.user_id DESC", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new User
                    {
                        user_id = reader.GetInt32(0),
                        role_id = reader.GetInt32(1),
                        username = reader.GetString(2),
                        email = reader.GetString(3),
                        password_hash = reader.GetString(4),
                        full_name = reader.GetString(5),
                        is_active = reader.GetBoolean(6),
                        last_login = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                        created_date = reader.GetDateTime(8)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_users'"))
                {
                    Console.WriteLine("tbl_users table doesn't exist yet.");
                    return users;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading users: {ex.Message}");
            }

            return users;
        }

        // CREATE - Add new user
        public async Task<int> CreateUserAsync(User user, string password)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                // Hash the password
                string hashedPassword = HashPassword(password);

                var command = new SqlCommand(@"
                    INSERT INTO tbl_users (role_id, username, email, password_hash, full_name, is_active, created_date) 
                    VALUES (@role_id, @username, @email, @password_hash, @full_name, @is_active, @created_date);
                    SELECT SCOPE_IDENTITY();", connection);

                command.Parameters.AddWithValue("@role_id", user.role_id);
                command.Parameters.AddWithValue("@username", user.username);
                command.Parameters.AddWithValue("@email", user.email);
                command.Parameters.AddWithValue("@password_hash", hashedPassword);
                command.Parameters.AddWithValue("@full_name", user.full_name);
                command.Parameters.AddWithValue("@is_active", user.is_active);
                command.Parameters.AddWithValue("@created_date", user.created_date);

                var result = await command.ExecuteScalarAsync();
                int userId = Convert.ToInt32(result);

                // Log audit action
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Create",
                        "tbl_users",
                        userId,
                        null,
                        new { username = user.username, email = user.email, full_name = user.full_name, role_id = user.role_id, is_active = user.is_active },
                        null,
                        null,
                        $"created user: {user.username}"
                    );
                }

                return userId;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_users'"))
                {
                    throw new Exception("tbl_users table doesn't exist. Please create the table first.");
                }
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key"))
                {
                    throw new Exception($"Username or email already exists.");
                }
                throw new Exception($"Error creating user: {ex.Message}");
            }
        }

        // UPDATE - Modify existing user
        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                // Get old values for audit log
                User? oldUser = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    try
                    {
                        oldUser = await GetUserByIdAsync(user.user_id);
                    }
                    catch
                    {
                        // If user not found, continue without old values
                    }
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    UPDATE tbl_users 
                    SET role_id = @role_id, 
                        username = @username, 
                        email = @email,
                        full_name = @full_name, 
                        is_active = @is_active
                    WHERE user_id = @user_id", connection);

                command.Parameters.AddWithValue("@user_id", user.user_id);
                command.Parameters.AddWithValue("@role_id", user.role_id);
                command.Parameters.AddWithValue("@username", user.username);
                command.Parameters.AddWithValue("@email", user.email);
                command.Parameters.AddWithValue("@full_name", user.full_name);
                command.Parameters.AddWithValue("@is_active", user.is_active);

                bool success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && oldUser != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update",
                        "tbl_users",
                        user.user_id,
                        new { username = oldUser.username, email = oldUser.email, full_name = oldUser.full_name, role_id = oldUser.role_id, is_active = oldUser.is_active },
                        new { username = user.username, email = user.email, full_name = user.full_name, role_id = user.role_id, is_active = user.is_active },
                        null,
                        null,
                        $"updated user: {user.username}"
                    );
                }

                return success;
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("UNIQUE KEY constraint") || ex.Message.Contains("duplicate key"))
                {
                    throw new Exception($"Username or email already exists.");
                }
                throw new Exception($"Error updating user: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating user: {ex.Message}");
            }
        }

        // UPDATE - Update user password
        public async Task<bool> UpdateUserPasswordAsync(int userId, string newPassword)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                string hashedPassword = HashPassword(newPassword);

                var command = new SqlCommand(@"
                    UPDATE tbl_users 
                    SET password_hash = @password_hash
                    WHERE user_id = @user_id", connection);

                command.Parameters.AddWithValue("@user_id", userId);
                command.Parameters.AddWithValue("@password_hash", hashedPassword);

                bool success = await command.ExecuteNonQueryAsync() > 0;

                // Log audit action (don't log password values for security)
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    // Get username for the message
                    var getUserCommand = new SqlCommand("SELECT username FROM tbl_users WHERE user_id = @user_id", connection);
                    getUserCommand.Parameters.AddWithValue("@user_id", userId);
                    var usernameResult = await getUserCommand.ExecuteScalarAsync();
                    string username = usernameResult?.ToString() ?? $"User ID {userId}";
                    
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Update Password",
                        "tbl_users",
                        userId,
                        null,
                        new { action = "Password changed" },
                        null,
                        null,
                        $"updated password for user: {username}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating password: {ex.Message}");
            }
        }

        // DELETE - Remove user
        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                // Get user details for audit log before deletion
                User? userToDelete = null;
                if (_auditLogService != null && _authService != null && _authService.IsAuthenticated)
                {
                    try
                    {
                        userToDelete = await GetUserByIdAsync(userId);
                    }
                    catch
                    {
                        // If user not found, continue without user details
                    }
                }

                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var deleteCommand = new SqlCommand("DELETE FROM tbl_users WHERE user_id = @user_id", connection);
                deleteCommand.Parameters.AddWithValue("@user_id", userId);

                bool success = await deleteCommand.ExecuteNonQueryAsync() > 0;

                // Log audit action
                if (success && _auditLogService != null && _authService != null && _authService.IsAuthenticated && userToDelete != null)
                {
                    await _auditLogService.LogActionAsync(
                        _authService.CurrentUserId,
                        "Delete",
                        "tbl_users",
                        userId,
                        new { username = userToDelete.username, email = userToDelete.email, full_name = userToDelete.full_name },
                        null,
                        null,
                        null,
                        $"deleted user: {userToDelete.username}"
                    );
                }

                return success;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting user: {ex.Message}");
            }
        }

        // READ - Get user by ID
        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT user_id, role_id, username, email, password_hash, full_name, is_active, last_login, created_date 
                    FROM tbl_users 
                    WHERE user_id = @user_id", connection);

                command.Parameters.AddWithValue("@user_id", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        user_id = reader.GetInt32(0),
                        role_id = reader.GetInt32(1),
                        username = reader.GetString(2),
                        email = reader.GetString(3),
                        password_hash = reader.GetString(4),
                        full_name = reader.GetString(5),
                        is_active = reader.GetBoolean(6),
                        last_login = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                        created_date = reader.GetDateTime(8)
                    };
                }

                throw new Exception("User not found");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading user: {ex.Message}");
            }
        }

        // READ - Get user by username
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT user_id, role_id, username, email, password_hash, full_name, is_active, last_login, created_date 
                    FROM tbl_users 
                    WHERE username = @username", connection);

                command.Parameters.AddWithValue("@username", username);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        user_id = reader.GetInt32(0),
                        role_id = reader.GetInt32(1),
                        username = reader.GetString(2),
                        email = reader.GetString(3),
                        password_hash = reader.GetString(4),
                        full_name = reader.GetString(5),
                        is_active = reader.GetBoolean(6),
                        last_login = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                        created_date = reader.GetDateTime(8)
                    };
                }

                throw new Exception("User not found");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading user: {ex.Message}");
            }
        }

        // Hash password using SHA256
        public string HashPassword(string password)
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
        public bool VerifyPassword(string password, string hash)
        {
            string hashOfInput = HashPassword(password);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }
    }
}