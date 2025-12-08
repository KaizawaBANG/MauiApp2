-- ============================================
-- Fix Sync Queue Constraint Name Conflict
-- ============================================
-- This script fixes the duplicate constraint name error
-- Run this on your cloud database if you got the CK_sync_status error
-- ============================================

PRINT 'Fixing sync queue constraint name...';
PRINT '';

-- Drop the existing constraint if it exists with wrong name
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_sync_status' AND parent_object_id = OBJECT_ID('tbl_sync_queue'))
BEGIN
    ALTER TABLE [dbo].[tbl_sync_queue]
    DROP CONSTRAINT [CK_sync_status];
    PRINT '✓ Dropped old CK_sync_status constraint from tbl_sync_queue';
END
GO

-- Add the constraint with correct name
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_sync_queue_status' AND parent_object_id = OBJECT_ID('tbl_sync_queue'))
BEGIN
    ALTER TABLE [dbo].[tbl_sync_queue]
    ADD CONSTRAINT [CK_sync_queue_status] CHECK ([sync_status] IN ('Pending', 'Syncing', 'Synced', 'Failed'));
    PRINT '✓ Added CK_sync_queue_status constraint to tbl_sync_queue';
END
ELSE
BEGIN
    PRINT '⚠ CK_sync_queue_status constraint already exists';
END
GO

-- Fix sync_history constraint name if needed
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_sync_status' AND parent_object_id = OBJECT_ID('tbl_sync_history'))
BEGIN
    ALTER TABLE [dbo].[tbl_sync_history]
    DROP CONSTRAINT [CK_sync_status];
    PRINT '✓ Dropped old CK_sync_status constraint from tbl_sync_history';
END
GO

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_sync_history_status' AND parent_object_id = OBJECT_ID('tbl_sync_history'))
BEGIN
    ALTER TABLE [dbo].[tbl_sync_history]
    ADD CONSTRAINT [CK_sync_history_status] CHECK ([status] IN ('Success', 'Failed', 'Partial'));
    PRINT '✓ Added CK_sync_history_status constraint to tbl_sync_history';
END
ELSE
BEGIN
    PRINT '⚠ CK_sync_history_status constraint already exists';
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Constraint Names Fixed!';
PRINT '========================================';
PRINT '';
PRINT 'Your sync tables are now properly configured.';
PRINT '';



