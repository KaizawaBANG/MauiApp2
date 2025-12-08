-- ============================================
-- Create tbl_sync_history Table
-- ============================================
-- This script creates the sync_history table that was missing
-- Run this on your cloud database
-- ============================================

PRINT 'Creating tbl_sync_history table...';
PRINT '';

-- ============================================
-- Create tbl_sync_history
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
    
    -- If table exists but constraint has wrong name, fix it
    IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_sync_status' AND parent_object_id = OBJECT_ID('tbl_sync_history'))
    BEGIN
        ALTER TABLE [dbo].[tbl_sync_history]
        DROP CONSTRAINT [CK_sync_status];
        PRINT '✓ Dropped old CK_sync_status constraint';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_sync_history_status' AND parent_object_id = OBJECT_ID('tbl_sync_history'))
    BEGIN
        ALTER TABLE [dbo].[tbl_sync_history]
        ADD CONSTRAINT [CK_sync_history_status] CHECK ([status] IN ('Success', 'Failed', 'Partial'));
        PRINT '✓ Added CK_sync_history_status constraint';
    END
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ tbl_sync_history Table Ready!';
PRINT '========================================';
PRINT '';
PRINT 'Your cloud database now has all sync tables:';
PRINT '  ✓ tbl_sync_queue';
PRINT '  ✓ tbl_sync_history';
PRINT '  ✓ tbl_connectivity_log';
PRINT '';



