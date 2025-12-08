-- Fix tax_id column in tbl_product to allow NULL values
-- Run this on your cloud database (db34089) to fix the sync error

USE [db34089];
GO

PRINT '=== Fixing tax_id column in tbl_product ===';
PRINT '';

-- Check if table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    -- Check if tax_id is currently NOT NULL
    IF EXISTS (
        SELECT * FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') 
        AND name = 'tax_id' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT 'Changing tax_id from NOT NULL to NULL...';
        
        -- Alter column to allow NULL
        ALTER TABLE [dbo].[tbl_product]
        ALTER COLUMN [tax_id] INT NULL;
        
        PRINT '✓ tax_id column now allows NULL values';
        PRINT '';
        PRINT 'You can now re-run the sync. Products with NULL tax_id will sync successfully.';
    END
    ELSE
    BEGIN
        PRINT '⚠ tax_id column already allows NULL values. No changes needed.';
    END
END
ELSE
BEGIN
    PRINT 'ERROR: tbl_product table does not exist!';
END
GO





