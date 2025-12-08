-- Quick Fix: Reset Product IDENTITY Seed
-- This script fixes the issue where new products get IDs like 1006 instead of continuing sequentially
-- 
-- IMPORTANT: This script does NOT delete any records. It only resets the identity seed.
-- If product_id 1006 is test data you want to remove, delete it first, then run this script.

PRINT '=== Fixing Product IDENTITY Seed ===';
PRINT '';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    -- Get the maximum product_id
    DECLARE @maxProductId INT;
    SELECT @maxProductId = ISNULL(MAX(product_id), 0) FROM tbl_product;
    
    PRINT 'Current maximum product_id: ' + CAST(@maxProductId AS NVARCHAR(10));
    
    -- Check for high IDs (>= 1000)
    DECLARE @highIdCount INT;
    SELECT @highIdCount = COUNT(*) FROM tbl_product WHERE product_id >= 1000;
    
    IF @highIdCount > 0
    BEGIN
        PRINT '⚠ WARNING: Found ' + CAST(@highIdCount AS NVARCHAR(10)) + ' product(s) with ID >= 1000';
        PRINT '   If these are test records, delete them first, then run this script again.';
        PRINT '';
    END
    
    -- Reset identity seed to the maximum ID
    DBCC CHECKIDENT ('tbl_product', RESEED, @maxProductId);
    
    PRINT '✓ IDENTITY seed reset successfully!';
    PRINT '  Next product ID will be: ' + CAST(@maxProductId + 1 AS NVARCHAR(10));
    PRINT '';
    PRINT 'To fix other tables (category, brand, tax, supplier, etc.),';
    PRINT 'run FixIdentitySeedsForAllTables.sql';
END
ELSE
BEGIN
    PRINT '❌ ERROR: tbl_product table does not exist!';
END
GO





