-- Add missing columns to existing tbl_supplier table
-- This script adds is_active, created_date, and modified_date columns

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND type in (N'U'))
BEGIN
    -- Add is_active column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND name = 'is_active')
    BEGIN
        ALTER TABLE [dbo].[tbl_supplier]
        ADD [is_active] BIT NOT NULL DEFAULT 1;
        
        PRINT 'is_active column added to tbl_supplier table.';
    END
    ELSE
    BEGIN
        PRINT 'is_active column already exists in tbl_supplier table.';
    END

    -- Add created_date column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND name = 'created_date')
    BEGIN
        ALTER TABLE [dbo].[tbl_supplier]
        ADD [created_date] DATETIME NOT NULL DEFAULT GETDATE();
        
        PRINT 'created_date column added to tbl_supplier table.';
    END
    ELSE
    BEGIN
        PRINT 'created_date column already exists in tbl_supplier table.';
    END

    -- Add modified_date column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND name = 'modified_date')
    BEGIN
        ALTER TABLE [dbo].[tbl_supplier]
        ADD [modified_date] DATETIME NULL;
        
        PRINT 'modified_date column added to tbl_supplier table.';
    END
    ELSE
    BEGIN
        PRINT 'modified_date column already exists in tbl_supplier table.';
    END
    
    PRINT '';
    PRINT 'All columns added successfully! ✅';
END
ELSE
BEGIN
    PRINT 'tbl_supplier table does not exist. Please create the table first.';
END
GO


