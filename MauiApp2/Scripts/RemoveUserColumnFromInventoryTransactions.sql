-- Remove processed_by column from inventory transaction tables
-- This script removes the user column from tbl_stock_in and tbl_stock_out tables

-- Remove processed_by column from tbl_stock_in
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND name = 'processed_by')
BEGIN
    -- Drop foreign key constraint first
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_stock_in_user')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in] DROP CONSTRAINT [FK_stock_in_user];
        PRINT 'Dropped FK_stock_in_user constraint';
    END

    -- Drop the column
    ALTER TABLE [dbo].[tbl_stock_in] DROP COLUMN [processed_by];
    PRINT 'Removed processed_by column from tbl_stock_in';
END
ELSE
BEGIN
    PRINT 'processed_by column does not exist in tbl_stock_in';
END
GO

-- Remove processed_by column from tbl_stock_out
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND name = 'processed_by')
BEGIN
    -- Drop foreign key constraint first
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_stock_out_user')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_out] DROP CONSTRAINT [FK_stock_out_user];
        PRINT 'Dropped FK_stock_out_user constraint';
    END

    -- Drop the column
    ALTER TABLE [dbo].[tbl_stock_out] DROP COLUMN [processed_by];
    PRINT 'Removed processed_by column from tbl_stock_out';
END
ELSE
BEGIN
    PRINT 'processed_by column does not exist in tbl_stock_out';
END
GO

PRINT 'Script completed successfully!';
GO



