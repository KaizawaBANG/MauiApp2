using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MauiApp2.Models;
using MauiApp2.Components.Database;

namespace MauiApp2.Services
{
    public interface IRoleService
    {
        Task<List<Role>> GetRolesAsync();
        Task<Role> GetRoleByIdAsync(int roleId);
    }

    public class RoleService : IRoleService
    {
        // READ - Get all roles
        public async Task<List<Role>> GetRolesAsync()
        {
            var roles = new List<Role>();

            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT role_id, role_name, description 
                    FROM tbl_roles 
                    ORDER BY role_name", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    roles.Add(new Role
                    {
                        role_id = reader.GetInt32(0),
                        role_name = reader.GetString(1),
                        description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Message.Contains("Invalid object name 'tbl_roles'"))
                {
                    Console.WriteLine("tbl_roles table doesn't exist yet.");
                    return roles;
                }
                throw new Exception($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading roles: {ex.Message}");
            }

            return roles;
        }

        // READ - Get role by ID
        public async Task<Role> GetRoleByIdAsync(int roleId)
        {
            try
            {
                using var connection = db.GetConnection();
                await connection.OpenAsync();

                var command = new SqlCommand(@"
                    SELECT role_id, role_name, description 
                    FROM tbl_roles 
                    WHERE role_id = @role_id", connection);

                command.Parameters.AddWithValue("@role_id", roleId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Role
                    {
                        role_id = reader.GetInt32(0),
                        role_name = reader.GetString(1),
                        description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    };
                }

                return new Role();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading role: {ex.Message}");
            }
        }
    }
}