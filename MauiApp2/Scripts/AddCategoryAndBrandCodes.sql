-- ============================================
-- Add Category Code and Brand Code Columns
-- ============================================
-- This script adds category_code and brand_code columns
-- to support improved SKU generation (CATEGORY-BRAND-NUMBER format)
-- ============================================

PRINT '========================================';
PRINT 'Adding Category and Brand Code Columns';
PRINT '========================================';
PRINT '';

-- ============================================
-- 1. Add category_code to tbl_category
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_category]') AND name = 'category_code')
BEGIN
    ALTER TABLE [dbo].[tbl_category]
    ADD [category_code] NVARCHAR(10) NULL;
    
    PRINT '✓ Added category_code column to tbl_category';
END
ELSE
BEGIN
    PRINT '⚠ category_code column already exists';
END
GO

-- Generate codes for existing categories (first 3-4 letters, uppercase)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_category]') AND name = 'category_code')
BEGIN
    UPDATE [dbo].[tbl_category]
    SET [category_code] = UPPER(LEFT(REPLACE(REPLACE(REPLACE(category_name, ' ', ''), '-', ''), '_', ''), 4))
    WHERE [category_code] IS NULL;
    
    PRINT '✓ Generated category codes for existing records';
    
    -- Make it NOT NULL after populating
    ALTER TABLE [dbo].[tbl_category]
    ALTER COLUMN [category_code] NVARCHAR(10) NOT NULL;
    
    -- Create unique index
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_category_code' AND object_id = OBJECT_ID(N'[dbo].[tbl_category]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_category_code] ON [dbo].[tbl_category] ([category_code]);
        PRINT '✓ Created unique index on category_code';
    END
END
GO

-- ============================================
-- 2. Add brand_code to tbl_brand
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND name = 'brand_code')
BEGIN
    ALTER TABLE [dbo].[tbl_brand]
    ADD [brand_code] NVARCHAR(10) NULL;
    
    PRINT '✓ Added brand_code column to tbl_brand';
END
ELSE
BEGIN
    PRINT '⚠ brand_code column already exists';
END
GO

-- Generate codes for existing brands (first 3 letters, uppercase)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND name = 'brand_code')
BEGIN
    UPDATE [dbo].[tbl_brand]
    SET [brand_code] = UPPER(LEFT(REPLACE(REPLACE(REPLACE(brand_name, ' ', ''), '-', ''), '_', ''), 3))
    WHERE [brand_code] IS NULL;
    
    -- Handle duplicates by adding numbers
    DECLARE @BrandId INT;
    DECLARE @BrandCode NVARCHAR(10);
    DECLARE @Counter INT;
    DECLARE brand_cursor CURSOR FOR
        SELECT brand_id, brand_code FROM [dbo].[tbl_brand] ORDER BY brand_id;
    
    OPEN brand_cursor;
    FETCH NEXT FROM brand_cursor INTO @BrandId, @BrandCode;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Check for duplicates
        IF (SELECT COUNT(*) FROM [dbo].[tbl_brand] WHERE brand_code = @BrandCode) > 1
        BEGIN
            SET @Counter = 1;
            WHILE EXISTS (SELECT 1 FROM [dbo].[tbl_brand] WHERE brand_code = @BrandCode + CAST(@Counter AS NVARCHAR(2)) AND brand_id != @BrandId)
            BEGIN
                SET @Counter = @Counter + 1;
            END
            UPDATE [dbo].[tbl_brand] SET brand_code = @BrandCode + CAST(@Counter AS NVARCHAR(2)) WHERE brand_id = @BrandId;
        END
        FETCH NEXT FROM brand_cursor INTO @BrandId, @BrandCode;
    END
    
    CLOSE brand_cursor;
    DEALLOCATE brand_cursor;
    
    PRINT '✓ Generated brand codes for existing records';
    
    -- Make it NOT NULL after populating
    ALTER TABLE [dbo].[tbl_brand]
    ALTER COLUMN [brand_code] NVARCHAR(10) NOT NULL;
    
    -- Create unique index
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_brand_code' AND object_id = OBJECT_ID(N'[dbo].[tbl_brand]'))
    BEGIN
        CREATE UNIQUE INDEX [IX_brand_code] ON [dbo].[tbl_brand] ([brand_code]);
        PRINT '✓ Created unique index on brand_code';
    END
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Category and Brand Code Columns Added!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Review generated codes in tbl_category and tbl_brand';
PRINT '  2. Update any codes that need adjustment';
PRINT '  3. New products will use format: CATEGORY-BRAND-NUMBER';
PRINT '';

