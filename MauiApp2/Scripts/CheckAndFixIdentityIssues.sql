-- Check and Fix Identity Issues
-- This script checks for identity seed mismatches and high ID records
-- Run this to diagnose issues before fixing them

PRINT '=== Checking Identity Seed Issues ===';
PRINT '';

-- Function to get current identity value
DECLARE @sql NVARCHAR(MAX);
DECLARE @currentIdentity INT;
DECLARE @maxId INT;

-- ============================================
-- Check tbl_product
-- ============================================
PRINT '--- tbl_product ---';
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    -- Get current identity value
    SELECT @currentIdentity = IDENT_CURRENT('tbl_product');
    SELECT @maxId = ISNULL(MAX(product_id), 0) FROM tbl_product;
    
    PRINT '  Current Identity Value: ' + CAST(@currentIdentity AS NVARCHAR(10));
    PRINT '  Maximum product_id: ' + CAST(@maxId AS NVARCHAR(10));
    
    IF @currentIdentity > @maxId
    BEGIN
        PRINT '  ⚠ ISSUE: Identity value (' + CAST(@currentIdentity AS NVARCHAR(10)) + ') is higher than max ID (' + CAST(@maxId AS NVARCHAR(10)) + ')';
        PRINT '  Next product will be ID: ' + CAST(@currentIdentity + 1 AS NVARCHAR(10));
    END
    ELSE
    BEGIN
        PRINT '  ✓ OK: Identity seed is correct';
    END
    
    -- Check for high IDs
    DECLARE @highIdCount INT;
    SELECT @highIdCount = COUNT(*) FROM tbl_product WHERE product_id >= 1000;
    IF @highIdCount > 0
    BEGIN
        PRINT '  ⚠ WARNING: ' + CAST(@highIdCount AS NVARCHAR(10)) + ' product(s) with ID >= 1000';
    END
END
PRINT '';

-- ============================================
-- Check tbl_supplier
-- ============================================
PRINT '--- tbl_supplier ---';
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND type in (N'U'))
BEGIN
    SELECT @currentIdentity = IDENT_CURRENT('tbl_supplier');
    SELECT @maxId = ISNULL(MAX(supplier_id), 0) FROM tbl_supplier;
    
    PRINT '  Current Identity Value: ' + CAST(@currentIdentity AS NVARCHAR(10));
    PRINT '  Maximum supplier_id: ' + CAST(@maxId AS NVARCHAR(10));
    
    IF @currentIdentity > @maxId
    BEGIN
        PRINT '  ⚠ ISSUE: Identity value (' + CAST(@currentIdentity AS NVARCHAR(10)) + ') is higher than max ID (' + CAST(@maxId AS NVARCHAR(10)) + ')';
        PRINT '  Next supplier will be ID: ' + CAST(@currentIdentity + 1 AS NVARCHAR(10));
        PRINT '  Run FixIdentitySeedsForAllTables.sql to fix this.';
    END
    ELSE
    BEGIN
        PRINT '  ✓ OK: Identity seed is correct';
    END
    
    SELECT @highIdCount = COUNT(*) FROM tbl_supplier WHERE supplier_id >= 1000;
    IF @highIdCount > 0
    BEGIN
        PRINT '  ⚠ WARNING: ' + CAST(@highIdCount AS NVARCHAR(10)) + ' supplier(s) with ID >= 1000';
    END
END
PRINT '';

-- ============================================
-- Check tbl_category
-- ============================================
PRINT '--- tbl_category ---';
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_category]') AND type in (N'U'))
BEGIN
    SELECT @currentIdentity = IDENT_CURRENT('tbl_category');
    SELECT @maxId = ISNULL(MAX(category_id), 0) FROM tbl_category;
    
    PRINT '  Current Identity Value: ' + CAST(@currentIdentity AS NVARCHAR(10));
    PRINT '  Maximum category_id: ' + CAST(@maxId AS NVARCHAR(10));
    
    IF @currentIdentity > @maxId
    BEGIN
        PRINT '  ⚠ ISSUE: Identity value is higher than max ID';
    END
    ELSE
    BEGIN
        PRINT '  ✓ OK: Identity seed is correct';
    END
END
PRINT '';

-- ============================================
-- Check tbl_brand
-- ============================================
PRINT '--- tbl_brand ---';
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
BEGIN
    SELECT @currentIdentity = IDENT_CURRENT('tbl_brand');
    SELECT @maxId = ISNULL(MAX(brand_id), 0) FROM tbl_brand;
    
    PRINT '  Current Identity Value: ' + CAST(@currentIdentity AS NVARCHAR(10));
    PRINT '  Maximum brand_id: ' + CAST(@maxId AS NVARCHAR(10));
    
    IF @currentIdentity > @maxId
    BEGIN
        PRINT '  ⚠ ISSUE: Identity value is higher than max ID';
    END
    ELSE
    BEGIN
        PRINT '  ✓ OK: Identity seed is correct';
    END
END
PRINT '';

-- ============================================
-- Check tbl_tax
-- ============================================
PRINT '--- tbl_tax ---';
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_tax]') AND type in (N'U'))
BEGIN
    SELECT @currentIdentity = IDENT_CURRENT('tbl_tax');
    SELECT @maxId = ISNULL(MAX(tax_id), 0) FROM tbl_tax;
    
    PRINT '  Current Identity Value: ' + CAST(@currentIdentity AS NVARCHAR(10));
    PRINT '  Maximum tax_id: ' + CAST(@maxId AS NVARCHAR(10));
    
    IF @currentIdentity > @maxId
    BEGIN
        PRINT '  ⚠ ISSUE: Identity value is higher than max ID';
    END
    ELSE
    BEGIN
        PRINT '  ✓ OK: Identity seed is correct';
    END
END
PRINT '';

PRINT '=== Check Complete ===';
PRINT '';
PRINT 'To fix issues, run: FixIdentitySeedsForAllTables.sql';
PRINT 'To delete high ID test records, run: DeleteProduct1006.sql or FixHighIdRecords.sql';
GO





