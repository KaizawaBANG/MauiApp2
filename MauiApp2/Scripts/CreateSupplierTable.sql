-- Create Supplier Table
-- This script creates the tbl_supplier table for managing suppliers

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_supplier] (
        [supplier_id] INT IDENTITY(1,1) PRIMARY KEY,
        [supplier_name] NVARCHAR(100) NOT NULL,
        [contact_number] NVARCHAR(20) NULL,
        [email] NVARCHAR(100) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL
    );

    -- Create index on supplier_name for faster lookups
    CREATE INDEX [IX_supplier_name] ON [dbo].[tbl_supplier] ([supplier_name]);

    -- Create unique constraint on email if provided
    CREATE UNIQUE INDEX [IX_supplier_email] ON [dbo].[tbl_supplier] ([email]) WHERE [email] IS NOT NULL;

    PRINT 'Supplier table created successfully!';
END
ELSE
BEGIN
    PRINT 'Supplier table already exists.';
END
GO


