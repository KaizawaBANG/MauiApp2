-- Drop Stock Adjustment Table
-- This table is not being used by the application (StockAdjustment.razor uses mock data only)
-- Safe to drop if you're not using stock adjustment functionality

PRINT '========================================';
PRINT 'Dropping Stock Adjustment Table';
PRINT '========================================';
PRINT '';

-- Check if table exists and drop it
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_adjustment]') AND type in (N'U'))
BEGIN
    -- Drop all constraints first (foreign keys, unique constraints, check constraints, etc.)
    DECLARE @sql NVARCHAR(MAX) = '';
    
    -- Drop foreign key constraints
    SELECT @sql = @sql + 'ALTER TABLE [dbo].[tbl_stock_adjustment] DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID('dbo.tbl_stock_adjustment');
    
    -- Drop unique constraints (including unique indexes)
    SELECT @sql = @sql + 'ALTER TABLE [dbo].[tbl_stock_adjustment] DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.tbl_stock_adjustment')
    AND type = 'UQ'; -- Unique constraints
    
    -- Drop check constraints
    SELECT @sql = @sql + 'ALTER TABLE [dbo].[tbl_stock_adjustment] DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.tbl_stock_adjustment');
    
    -- Drop default constraints
    SELECT @sql = @sql + 'ALTER TABLE [dbo].[tbl_stock_adjustment] DROP CONSTRAINT ' + QUOTENAME(name) + ';' + CHAR(13)
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.tbl_stock_adjustment');
    
    IF @sql <> ''
    BEGIN
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped all constraints';
    END
    
    -- Drop remaining non-clustered indexes (if any remain after dropping constraints)
    SET @sql = '';
    SELECT @sql = @sql + 'DROP INDEX ' + QUOTENAME(name) + ' ON [dbo].[tbl_stock_adjustment];' + CHAR(13)
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.tbl_stock_adjustment')
    AND name IS NOT NULL
    AND is_primary_key = 0
    AND type_desc = 'NONCLUSTERED';
    
    IF @sql <> ''
    BEGIN
        EXEC sp_executesql @sql;
        PRINT '  ✓ Dropped remaining indexes';
    END
    
    -- Drop the table
    DROP TABLE [dbo].[tbl_stock_adjustment];
    PRINT '  ✓ Dropped tbl_stock_adjustment table';
    PRINT '';
    PRINT 'Stock Adjustment table has been successfully removed from the database.';
END
ELSE
BEGIN
    PRINT '  ⚠ tbl_stock_adjustment table does not exist. Nothing to drop.';
END
GO

PRINT '';
PRINT '========================================';
PRINT 'Done';
PRINT '========================================';
GO

