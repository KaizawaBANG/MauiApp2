-- Add Description column to Audit Log Table
-- This allows storing descriptive action messages separately from the action type

PRINT '=== Adding Description Column to Audit Log Table ===';
PRINT '';

-- Check if column already exists
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('tbl_audit_log') AND name = 'description')
BEGIN
    ALTER TABLE tbl_audit_log
    ADD description NVARCHAR(500) NULL;

    PRINT '✓ Added description column to tbl_audit_log.';
    
    -- Create index on description for better search performance
    CREATE INDEX [IX_audit_log_description] ON [dbo].[tbl_audit_log] ([description]);
    
    PRINT '✓ Created index on description column.';
END
ELSE
BEGIN
    PRINT '⚠ description column already exists in tbl_audit_log.';
END
GO

PRINT '';
PRINT '=== Description Column Addition Complete ===';



