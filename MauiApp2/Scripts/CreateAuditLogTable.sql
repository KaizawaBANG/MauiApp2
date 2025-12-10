-- Create Audit Log Table
-- This table stores all audit logs for user actions and system events

PRINT '=== Creating Audit Log Table ===';
PRINT '';

-- ============================================
-- Create tbl_audit_log
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_audit_log]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_audit_log] (
        [log_id] INT IDENTITY(1,1) PRIMARY KEY,
        [user_id] INT NOT NULL,
        [action_type] NVARCHAR(50) NOT NULL, -- Create, Update, Delete, View, Login, Logout, Export, Print
        [table_name] NVARCHAR(100) NULL,
        [record_id] INT NULL,
        [old_values] NVARCHAR(MAX) NULL,
        [new_values] NVARCHAR(MAX) NULL,
        [ip_address] NVARCHAR(50) NULL,
        [user_agent] NVARCHAR(500) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_audit_log_users] FOREIGN KEY ([user_id]) REFERENCES [dbo].[tbl_users]([user_id])
    );
    
    CREATE INDEX [IX_audit_log_user_id] ON [dbo].[tbl_audit_log] ([user_id]);
    CREATE INDEX [IX_audit_log_action_type] ON [dbo].[tbl_audit_log] ([action_type]);
    CREATE INDEX [IX_audit_log_table_name] ON [dbo].[tbl_audit_log] ([table_name]);
    CREATE INDEX [IX_audit_log_created_date] ON [dbo].[tbl_audit_log] ([created_date]);
    CREATE INDEX [IX_audit_log_record_id] ON [dbo].[tbl_audit_log] ([record_id]);
    
    PRINT '✓ Created tbl_audit_log';
END
ELSE
BEGIN
    PRINT '⚠ tbl_audit_log already exists';
END
GO

PRINT '';
PRINT '=== Audit Log Table Creation Complete ===';





