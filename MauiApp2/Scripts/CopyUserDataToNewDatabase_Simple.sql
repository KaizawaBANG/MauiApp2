-- Simple Script: Copy User Data to New Database
-- Run this script while connected to your NEW database
-- 
-- INSTRUCTIONS:
-- 1. Replace "YourOldDatabaseName" with your actual old database name
-- 2. Make sure you're connected to the NEW database when running this
-- 3. Make sure tbl_roles and tbl_users tables exist in the new database first

USE [YourNewDatabaseName];  -- Change this to your new database name
GO

PRINT '=== Copying User Data from Old Database ===';
PRINT '';

-- ============================================
-- Step 1: Copy Roles (if not already copied)
-- ============================================
PRINT 'Copying roles...';

INSERT INTO [dbo].[tbl_roles] ([role_id], [role_name], [description])
SELECT [role_id], [role_name], [description]
FROM [YourOldDatabaseName].[dbo].[tbl_roles]  -- Change this to your old database name
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[tbl_roles] t 
    WHERE t.role_id = [YourOldDatabaseName].[dbo].[tbl_roles].role_id  -- Change this too
);

PRINT '✓ Roles copied';
PRINT '';

-- ============================================
-- Step 2: Copy Users (including password hashes)
-- ============================================
PRINT 'Copying users with password hashes...';

INSERT INTO [dbo].[tbl_users] 
    ([user_id], [username], [password_hash], [full_name], [email], [role_id], [is_active], [last_login], [created_date])
SELECT 
    [user_id], 
    [username], 
    [password_hash],  -- Password hash copied as-is - users can log in with same passwords
    [full_name], 
    [email], 
    [role_id], 
    [is_active], 
    [last_login], 
    [created_date]
FROM [YourOldDatabaseName].[dbo].[tbl_users]  -- Change this to your old database name
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[tbl_users] t 
    WHERE t.user_id = [YourOldDatabaseName].[dbo].[tbl_users].user_id  -- Change this too
);

DECLARE @usersCopied INT = @@ROWCOUNT;
PRINT '✓ Copied ' + CAST(@usersCopied AS NVARCHAR(10)) + ' user(s)';
PRINT '';

-- ============================================
-- Step 3: Reset Identity Seed
-- ============================================
PRINT 'Resetting identity seed...';

DECLARE @maxUserId INT;
SELECT @maxUserId = ISNULL(MAX([user_id]), 0) FROM [dbo].[tbl_users];
DBCC CHECKIDENT ('[dbo].[tbl_users]', RESEED, @maxUserId);

PRINT '✓ Identity seed reset. Next user ID will be: ' + CAST(@maxUserId + 1 AS NVARCHAR(10));
PRINT '';

PRINT '=== Complete! ===';
PRINT 'All users have been copied with their password hashes.';
PRINT 'Users can now log in with the same usernames and passwords.';
GO





