-- ============================================
-- Database Restore Script - Revert Cleanup
-- ============================================
-- This script restores your database from a backup
-- Use this to revert the cleanup and restore all data
-- ============================================

-- IMPORTANT: Change these variables to match your backup file
DECLARE @DatabaseName NVARCHAR(128) = DB_NAME(); -- Current database
DECLARE @BackupFilePath NVARCHAR(260) = 'C:\Backup\YourDatabaseName_Backup_20250115_120000.bak'; -- CHANGE THIS to your backup file path

PRINT '========================================';
PRINT 'Database Restore - Revert Cleanup';
PRINT '========================================';
PRINT '';
PRINT 'WARNING: This will REPLACE your current database!';
PRINT 'All current data will be lost and replaced with backup data.';
PRINT '';
PRINT 'Database: ' + @DatabaseName;
PRINT 'Backup File: ' + @BackupFilePath;
PRINT '';
PRINT 'Press Ctrl+C to cancel, or wait 10 seconds to continue...';
PRINT '';

WAITFOR DELAY '00:00:10';

-- Check if backup file exists
DECLARE @FileExists INT;
EXEC xp_fileexist @BackupFilePath, @FileExists OUTPUT;

IF @FileExists = 0
BEGIN
    PRINT '❌ Error: Backup file not found!';
    PRINT 'File: ' + @BackupFilePath;
    PRINT '';
    PRINT 'Please:';
    PRINT '  1. Check the file path is correct';
    PRINT '  2. Make sure the backup file exists';
    PRINT '  3. Update @BackupFilePath variable in this script';
    PRINT '';
    RETURN;
END

BEGIN TRY
    PRINT 'Starting restore...';
    PRINT '';

    -- Set database to single user mode (disconnect all users)
    PRINT 'Setting database to single user mode...';
    ALTER DATABASE @DatabaseName SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    PRINT '  ✓ Database set to single user mode';
    PRINT '';

    -- Restore database
    PRINT 'Restoring database from backup...';
    RESTORE DATABASE @DatabaseName
    FROM DISK = @BackupFilePath
    WITH REPLACE, -- Replace existing database
         RECOVERY, -- Bring database online
         STATS = 10;

    -- Set database back to multi-user mode
    PRINT '';
    PRINT 'Setting database back to multi-user mode...';
    ALTER DATABASE @DatabaseName SET MULTI_USER;
    PRINT '  ✓ Database set to multi-user mode';
    PRINT '';

    PRINT '';
    PRINT '========================================';
    PRINT '✅ Database Restored Successfully!';
    PRINT '========================================';
    PRINT 'Your database has been restored from backup.';
    PRINT 'All data from the backup has been restored.';
    PRINT '';

END TRY
BEGIN CATCH
    -- Try to set database back to multi-user mode even if restore failed
    BEGIN TRY
        ALTER DATABASE @DatabaseName SET MULTI_USER;
    END TRY
    BEGIN CATCH
        -- Ignore error if database doesn't exist
    END CATCH

    PRINT '';
    PRINT '========================================';
    PRINT '❌ Restore Failed!';
    PRINT '========================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '';
    PRINT 'Possible causes:';
    PRINT '  - Backup file is corrupted';
    PRINT '  - Database is in use by other connections';
    PRINT '  - Insufficient permissions';
    PRINT '  - Disk space issues';
    PRINT '';
    PRINT 'Try:';
    PRINT '  1. Close all connections to the database';
    PRINT '  2. Run this script again';
    PRINT '  3. Check backup file integrity';
    PRINT '';
END CATCH;
GO




