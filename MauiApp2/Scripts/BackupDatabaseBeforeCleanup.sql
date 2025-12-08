-- ============================================
-- Database Backup Script - Before Cleanup
-- ============================================
-- This script creates a backup of your database BEFORE cleanup
-- Use this to restore your database if you need to revert changes
-- ============================================

-- IMPORTANT: Change these variables to match your setup
DECLARE @DatabaseName NVARCHAR(128) = DB_NAME(); -- Current database
DECLARE @BackupPath NVARCHAR(260) = 'C:\Backup\'; -- Backup folder path
DECLARE @BackupFileName NVARCHAR(260);
DECLARE @BackupDate NVARCHAR(20) = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');

-- Create backup file name with timestamp
SET @BackupFileName = @BackupPath + @DatabaseName + '_Backup_' + @BackupDate + '.bak';

PRINT '========================================';
PRINT 'Database Backup - Before Cleanup';
PRINT '========================================';
PRINT '';
PRINT 'Database: ' + @DatabaseName;
PRINT 'Backup Path: ' + @BackupFileName;
PRINT '';

-- Check if backup folder exists (create if needed)
DECLARE @FolderExists INT;
EXEC xp_fileexist @BackupPath, @FolderExists OUTPUT;

IF @FolderExists = 0
BEGIN
    PRINT '⚠ Warning: Backup folder does not exist: ' + @BackupPath;
    PRINT 'Please create the folder manually or change @BackupPath variable.';
    PRINT '';
    PRINT 'Example: Create folder C:\Backup\ before running this script.';
    PRINT '';
    RETURN;
END

BEGIN TRY
    -- Create backup name variable
    DECLARE @BackupName NVARCHAR(260) = @DatabaseName + '_Full_Backup_' + @BackupDate;

    -- Create backup
    PRINT 'Creating backup...';
    BACKUP DATABASE @DatabaseName
    TO DISK = @BackupFileName
    WITH FORMAT,
         NAME = @BackupName,
         DESCRIPTION = 'Full backup before database cleanup',
         COMPRESSION,
         STATS = 10;

    PRINT '';
    PRINT '========================================';
    PRINT '✅ Backup Created Successfully!';
    PRINT '========================================';
    PRINT 'Backup File: ' + @BackupFileName;
    PRINT '';
    PRINT 'To restore this backup later, use:';
    PRINT '  RestoreDatabaseFromBackup.sql';
    PRINT '  Or use SQL Server Management Studio';
    PRINT '';

END TRY
BEGIN CATCH
    PRINT '';
    PRINT '========================================';
    PRINT '❌ Backup Failed!';
    PRINT '========================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '';
    PRINT 'Possible causes:';
    PRINT '  - Backup folder does not exist';
    PRINT '  - Insufficient disk space';
    PRINT '  - Permission issues';
    PRINT '';
END CATCH;
GO

