-- ============================================
-- Create Sync Tables for Cloud Database
-- ============================================
-- Run this script on your cloud database to create
-- sync queue, sync history, and connectivity log tables
-- These tables support offline mode synchronization
-- ============================================

PRINT '========================================';
PRINT 'Creating Sync Tables for Cloud Database';
PRINT '========================================';
PRINT '';

-- ============================================
-- 1. Create tbl_sync_queue
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sync_queue]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_sync_queue] (
        [queue_id] INT IDENTITY(1,1) PRIMARY KEY,
        [table_name] NVARCHAR(100) NOT NULL,
        [operation_type] NVARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
        [record_id] INT NOT NULL,
        [record_data] NVARCHAR(MAX) NULL, -- JSON of record data for reference
        [sync_status] NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Syncing, Synced, Failed
        [error_message] NVARCHAR(MAX) NULL,
        [retry_count] INT NOT NULL DEFAULT 0,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [synced_date] DATETIME NULL,
        [last_attempt_date] DATETIME NULL,
        CONSTRAINT [CK_operation_type] CHECK ([operation_type] IN ('INSERT', 'UPDATE', 'DELETE')),
        CONSTRAINT [CK_sync_queue_status] CHECK ([sync_status] IN ('Pending', 'Syncing', 'Synced', 'Failed'))
    );
    
    CREATE INDEX [IX_sync_queue_status] ON [dbo].[tbl_sync_queue] ([sync_status]);
    CREATE INDEX [IX_sync_queue_table] ON [dbo].[tbl_sync_queue] ([table_name], [record_id]);
    CREATE INDEX [IX_sync_queue_created] ON [dbo].[tbl_sync_queue] ([created_date]);
    PRINT '✓ Created tbl_sync_queue';
END
ELSE
BEGIN
    PRINT '⚠ tbl_sync_queue already exists';
END
GO

-- ============================================
-- 2. Create tbl_sync_history
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sync_history]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_sync_history] (
        [sync_id] INT IDENTITY(1,1) PRIMARY KEY,
        [sync_start] DATETIME NOT NULL DEFAULT GETDATE(),
        [sync_end] DATETIME NULL,
        [status] NVARCHAR(20) NOT NULL, -- Success, Failed, Partial
        [tables_synced] INT NOT NULL DEFAULT 0,
        [records_synced] INT NOT NULL DEFAULT 0,
        [queue_items_processed] INT NOT NULL DEFAULT 0,
        [errors_count] INT NOT NULL DEFAULT 0,
        [error_details] NVARCHAR(MAX) NULL,
        [duration_seconds] INT NULL,
        [created_by] INT NULL,
        CONSTRAINT [FK_sync_history_user] FOREIGN KEY ([created_by]) REFERENCES [dbo].[tbl_users]([user_id]),
        CONSTRAINT [CK_sync_history_status] CHECK ([status] IN ('Success', 'Failed', 'Partial'))
    );
    
    CREATE INDEX [IX_sync_history_date] ON [dbo].[tbl_sync_history] ([sync_start]);
    CREATE INDEX [IX_sync_history_status] ON [dbo].[tbl_sync_history] ([status]);
    PRINT '✓ Created tbl_sync_history';
END
ELSE
BEGIN
    PRINT '⚠ tbl_sync_history already exists';
END
GO

-- ============================================
-- 3. Create tbl_connectivity_log
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_connectivity_log]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_connectivity_log] (
        [log_id] INT IDENTITY(1,1) PRIMARY KEY,
        [is_online] BIT NOT NULL,
        [check_time] DATETIME NOT NULL DEFAULT GETDATE(),
        [response_time_ms] INT NULL,
        [error_message] NVARCHAR(500) NULL
    );
    
    CREATE INDEX [IX_connectivity_log_time] ON [dbo].[tbl_connectivity_log] ([check_time]);
    PRINT '✓ Created tbl_connectivity_log';
END
ELSE
BEGIN
    PRINT '⚠ tbl_connectivity_log already exists';
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Sync Tables Created Successfully!';
PRINT '========================================';
PRINT '';
PRINT 'Tables created:';
PRINT '  ✓ tbl_sync_queue - Tracks operations to sync';
PRINT '  ✓ tbl_sync_history - Logs sync operations';
PRINT '  ✓ tbl_connectivity_log - Logs connectivity checks';
PRINT '';
PRINT 'Note: These tables are also included in CompleteCloudDatabaseSetup.sql';
PRINT 'If you already ran that script, these tables should already exist.';
PRINT '';

