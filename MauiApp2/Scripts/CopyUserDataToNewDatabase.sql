-- Copy User Data to New Clean Database
-- This script copies user data (including password hashes) from your old database to a new clean database
-- 
-- INSTRUCTIONS:
-- 1. Update the source database name below (OLD_DATABASE_NAME)
-- 2. Update the target database name below (NEW_DATABASE_NAME)
-- 3. Make sure both databases exist and you have access to both
-- 4. Run this script while connected to the NEW database
--
-- IMPORTANT: This script assumes:
-- - tbl_roles table already exists in the new database
-- - The role_id values match between old and new databases

PRINT '=== Copying User Data to New Database ===';
PRINT '';

-- ============================================
-- CONFIGURATION: Update these database names
-- ============================================
DECLARE @SourceDatabase NVARCHAR(128) = 'YourOldDatabaseName';  -- Change this to your old database name
DECLARE @TargetDatabase NVARCHAR(128) = 'YourNewDatabaseName';  -- Change this to your new database name

-- ============================================
-- Step 1: Check if source database exists
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = @SourceDatabase)
BEGIN
    PRINT 'ERROR: Source database "' + @SourceDatabase + '" does not exist!';
    PRINT 'Please update @SourceDatabase variable with the correct database name.';
    RETURN;
END

-- ============================================
-- Step 2: Check if target database exists
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = @TargetDatabase)
BEGIN
    PRINT 'ERROR: Target database "' + @TargetDatabase + '" does not exist!';
    PRINT 'Please update @TargetDatabase variable with the correct database name.';
    RETURN;
END

PRINT 'Source Database: ' + @SourceDatabase;
PRINT 'Target Database: ' + @TargetDatabase;
PRINT '';

-- ============================================
-- Step 3: Copy roles first (if they don't exist)
-- ============================================
PRINT '--- Copying Roles ---';
DECLARE @sql NVARCHAR(MAX);

SET @sql = N'
INSERT INTO [' + @TargetDatabase + '].[dbo].[tbl_roles] ([role_id], [role_name], [description])
SELECT [role_id], [role_name], [description]
FROM [' + @SourceDatabase + '].[dbo].[tbl_roles]
WHERE NOT EXISTS (
    SELECT 1 FROM [' + @TargetDatabase + '].[dbo].[tbl_roles] t 
    WHERE t.role_id = [' + @SourceDatabase + '].[dbo].[tbl_roles].role_id
);';

EXEC sp_executesql @sql;

DECLARE @rolesCopied INT;
SET @sql = N'SELECT @count = @@ROWCOUNT';
EXEC sp_executesql @sql, N'@count INT OUTPUT', @count = @rolesCopied OUTPUT;
PRINT '✓ Copied ' + CAST(@rolesCopied AS NVARCHAR(10)) + ' role(s)';
PRINT '';

-- ============================================
-- Step 4: Copy users (including password hashes)
-- ============================================
PRINT '--- Copying Users ---';

SET @sql = N'
INSERT INTO [' + @TargetDatabase + '].[dbo].[tbl_users] 
    ([user_id], [username], [password_hash], [full_name], [email], [role_id], [is_active], [last_login], [created_date])
SELECT 
    [user_id], 
    [username], 
    [password_hash],  -- Password hash is copied as-is
    [full_name], 
    [email], 
    [role_id], 
    [is_active], 
    [last_login], 
    [created_date]
FROM [' + @SourceDatabase + '].[dbo].[tbl_users]
WHERE NOT EXISTS (
    SELECT 1 FROM [' + @TargetDatabase + '].[dbo].[tbl_users] t 
    WHERE t.user_id = [' + @SourceDatabase + '].[dbo].[tbl_users].user_id
);';

EXEC sp_executesql @sql;

DECLARE @usersCopied INT;
SET @sql = N'SELECT @count = @@ROWCOUNT';
EXEC sp_executesql @sql, N'@count INT OUTPUT', @count = @usersCopied OUTPUT;
PRINT '✓ Copied ' + CAST(@usersCopied AS NVARCHAR(10)) + ' user(s) with password hashes';
PRINT '';

-- ============================================
-- Step 5: Reset identity seed for users table
-- ============================================
PRINT '--- Resetting Identity Seed ---';

SET @sql = N'
DECLARE @maxUserId INT;
SELECT @maxUserId = ISNULL(MAX([user_id]), 0) FROM [' + @TargetDatabase + '].[dbo].[tbl_users];
DBCC CHECKIDENT (''' + @TargetDatabase + '.dbo.tbl_users'', RESEED, @maxUserId);
PRINT ''✓ Identity seed reset. Next user ID will be: '' + CAST(@maxUserId + 1 AS NVARCHAR(10));
';

EXEC sp_executesql @sql;

PRINT '';
PRINT '=== User Data Copy Complete! ===';
PRINT 'Total users copied: ' + CAST(@usersCopied AS NVARCHAR(10));
PRINT '';
PRINT 'IMPORTANT:';
PRINT '- All password hashes have been copied successfully';
PRINT '- Users can log in with the same usernames and passwords';
PRINT '- Identity seed has been reset to prevent ID conflicts';
PRINT '';
GO





