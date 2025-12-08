-- Add Rejected Items columns to tbl_stock_in_items
-- This allows tracking of rejected/damaged items during stock in

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
BEGIN
    -- Add quantity_rejected column
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND name = 'quantity_rejected')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in_items]
        ADD [quantity_rejected] INT NOT NULL DEFAULT 0;
        PRINT 'quantity_rejected column added to tbl_stock_in_items table.';
    END
    ELSE
    BEGIN
        PRINT 'quantity_rejected column already exists in tbl_stock_in_items table.';
    END

    -- Add rejection_reason column
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND name = 'rejection_reason')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in_items]
        ADD [rejection_reason] NVARCHAR(100) NULL;
        PRINT 'rejection_reason column added to tbl_stock_in_items table.';
    END
    ELSE
    BEGIN
        PRINT 'rejection_reason column already exists in tbl_stock_in_items table.';
    END

    -- Add rejection_remarks column
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND name = 'rejection_remarks')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in_items]
        ADD [rejection_remarks] NVARCHAR(MAX) NULL;
        PRINT 'rejection_remarks column added to tbl_stock_in_items table.';
    END
    ELSE
    BEGIN
        PRINT 'rejection_remarks column already exists in tbl_stock_in_items table.';
    END

    PRINT '';
END
GO

-- Add check constraint to ensure quantity_rejected >= 0 (only if column exists)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND name = 'quantity_rejected')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_quantity_rejected')
        BEGIN
            ALTER TABLE [dbo].[tbl_stock_in_items]
            ADD CONSTRAINT [CK_quantity_rejected] CHECK ([quantity_rejected] >= 0);
            PRINT 'Check constraint CK_quantity_rejected added.';
        END
        ELSE
        BEGIN
            PRINT 'Check constraint CK_quantity_rejected already exists.';
        END
    END

    PRINT 'All rejected items columns and constraints added successfully! ✅';
END
ELSE
BEGIN
    PRINT 'tbl_stock_in_items table does not exist. Please create the table first.';
END
GO

