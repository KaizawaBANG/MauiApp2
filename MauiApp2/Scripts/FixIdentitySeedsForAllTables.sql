-- Fix IDENTITY Seeds for All Tables
-- This script resets IDENTITY seeds to continue sequentially from the maximum existing ID
-- Run this script if you notice IDs jumping to 1000+ when adding new records
-- 
-- IMPORTANT: This script does NOT delete any records. It only resets the identity seed.
-- If you want to delete high ID records (>= 1000), use FixHighIdRecords.sql instead.

PRINT '=== Fixing IDENTITY Seeds for All Tables ===';
PRINT 'This will reset identity seeds to continue from the maximum existing ID in each table.';
PRINT '';

-- ============================================
-- Fix tbl_roles IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_roles]') AND type in (N'U'))
BEGIN
    DECLARE @maxRoleId INT;
    SELECT @maxRoleId = ISNULL(MAX(role_id), 0) FROM tbl_roles;
    
    DBCC CHECKIDENT ('tbl_roles', RESEED, @maxRoleId);
    PRINT '✓ Fixed tbl_roles IDENTITY seed. Current max ID: ' + CAST(@maxRoleId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_roles table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_users IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_users]') AND type in (N'U'))
BEGIN
    DECLARE @maxUserId INT;
    SELECT @maxUserId = ISNULL(MAX(user_id), 0) FROM tbl_users;
    
    DBCC CHECKIDENT ('tbl_users', RESEED, @maxUserId);
    PRINT '✓ Fixed tbl_users IDENTITY seed. Current max ID: ' + CAST(@maxUserId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_users table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_category IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_category]') AND type in (N'U'))
BEGIN
    DECLARE @maxCategoryId INT;
    SELECT @maxCategoryId = ISNULL(MAX(category_id), 0) FROM tbl_category;
    
    DBCC CHECKIDENT ('tbl_category', RESEED, @maxCategoryId);
    PRINT '✓ Fixed tbl_category IDENTITY seed. Current max ID: ' + CAST(@maxCategoryId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_category table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_brand IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
BEGIN
    DECLARE @maxBrandId INT;
    SELECT @maxBrandId = ISNULL(MAX(brand_id), 0) FROM tbl_brand;
    
    DBCC CHECKIDENT ('tbl_brand', RESEED, @maxBrandId);
    PRINT '✓ Fixed tbl_brand IDENTITY seed. Current max ID: ' + CAST(@maxBrandId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_brand table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_tax IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_tax]') AND type in (N'U'))
BEGIN
    DECLARE @maxTaxId INT;
    SELECT @maxTaxId = ISNULL(MAX(tax_id), 0) FROM tbl_tax;
    
    DBCC CHECKIDENT ('tbl_tax', RESEED, @maxTaxId);
    PRINT '✓ Fixed tbl_tax IDENTITY seed. Current max ID: ' + CAST(@maxTaxId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_tax table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_product IDENTITY (Main Issue)
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    DECLARE @maxProductId INT;
    SELECT @maxProductId = ISNULL(MAX(product_id), 0) FROM tbl_product;
    
    PRINT '';
    PRINT '--- tbl_product Analysis ---';
    PRINT 'Current maximum product_id: ' + CAST(@maxProductId AS NVARCHAR(10));
    
    -- Check for high IDs
    DECLARE @highIdCount INT;
    SELECT @highIdCount = COUNT(*) FROM tbl_product WHERE product_id >= 1000;
    IF @highIdCount > 0
    BEGIN
        PRINT '⚠ WARNING: Found ' + CAST(@highIdCount AS NVARCHAR(10)) + ' product(s) with ID >= 1000';
        PRINT '   If these are test records, consider deleting them first using FixHighIdRecords.sql';
    END
    
    DBCC CHECKIDENT ('tbl_product', RESEED, @maxProductId);
    PRINT '✓ Fixed tbl_product IDENTITY seed. Next product ID will be: ' + CAST(@maxProductId + 1 AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_product table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_supplier IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND type in (N'U'))
BEGIN
    DECLARE @maxSupplierId INT;
    SELECT @maxSupplierId = ISNULL(MAX(supplier_id), 0) FROM tbl_supplier;
    
    DBCC CHECKIDENT ('tbl_supplier', RESEED, @maxSupplierId);
    PRINT '✓ Fixed tbl_supplier IDENTITY seed. Current max ID: ' + CAST(@maxSupplierId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_supplier table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_purchase_order IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND type in (N'U'))
BEGIN
    DECLARE @maxPOId INT;
    SELECT @maxPOId = ISNULL(MAX(po_id), 0) FROM tbl_purchase_order;
    
    DBCC CHECKIDENT ('tbl_purchase_order', RESEED, @maxPOId);
    PRINT '✓ Fixed tbl_purchase_order IDENTITY seed. Current max ID: ' + CAST(@maxPOId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_purchase_order table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_purchase_order_items IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
BEGIN
    DECLARE @maxPOItemId INT = 0;
    DECLARE @identityColumnName NVARCHAR(128) = NULL;
    
    -- Find the identity column name
    SELECT @identityColumnName = c.name
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'tbl_purchase_order_items'
      AND c.is_identity = 1;
    
    IF @identityColumnName IS NOT NULL
    BEGIN
        -- Dynamically get the max ID based on the actual column name
        DECLARE @sql NVARCHAR(MAX);
        SET @sql = N'SELECT @maxId = ISNULL(MAX([' + @identityColumnName + N']), 0) FROM tbl_purchase_order_items';
        EXEC sp_executesql @sql, N'@maxId INT OUTPUT', @maxId = @maxPOItemId OUTPUT;
        
        DBCC CHECKIDENT ('tbl_purchase_order_items', RESEED, @maxPOItemId);
        PRINT '✓ Fixed tbl_purchase_order_items IDENTITY seed (' + @identityColumnName + '). Current max ID: ' + CAST(@maxPOItemId AS NVARCHAR(10));
    END
    ELSE
    BEGIN
        PRINT '⚠ Could not find identity column in tbl_purchase_order_items. Skipping...';
    END
END
ELSE
BEGIN
    PRINT '⚠ tbl_purchase_order_items table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_stock_in IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
BEGIN
    DECLARE @maxStockInId INT;
    SELECT @maxStockInId = ISNULL(MAX(stock_in_id), 0) FROM tbl_stock_in;
    
    DBCC CHECKIDENT ('tbl_stock_in', RESEED, @maxStockInId);
    PRINT '✓ Fixed tbl_stock_in IDENTITY seed. Current max ID: ' + CAST(@maxStockInId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_in table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_stock_in_items IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
BEGIN
    DECLARE @maxStockInItemId INT;
    SELECT @maxStockInItemId = ISNULL(MAX(stock_in_items_id), 0) FROM tbl_stock_in_items;
    
    DBCC CHECKIDENT ('tbl_stock_in_items', RESEED, @maxStockInItemId);
    PRINT '✓ Fixed tbl_stock_in_items IDENTITY seed. Current max ID: ' + CAST(@maxStockInItemId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_in_items table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_sales_order IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
BEGIN
    DECLARE @maxSalesOrderId INT;
    SELECT @maxSalesOrderId = ISNULL(MAX(sales_order_id), 0) FROM tbl_sales_order;
    
    DBCC CHECKIDENT ('tbl_sales_order', RESEED, @maxSalesOrderId);
    PRINT '✓ Fixed tbl_sales_order IDENTITY seed. Current max ID: ' + CAST(@maxSalesOrderId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_sales_order table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_sales_order_items IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
BEGIN
    DECLARE @maxSalesOrderItemId INT;
    SELECT @maxSalesOrderItemId = ISNULL(MAX(sales_order_item_id), 0) FROM tbl_sales_order_items;
    
    DBCC CHECKIDENT ('tbl_sales_order_items', RESEED, @maxSalesOrderItemId);
    PRINT '✓ Fixed tbl_sales_order_items IDENTITY seed. Current max ID: ' + CAST(@maxSalesOrderItemId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_sales_order_items table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_stock_out IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
BEGIN
    DECLARE @maxStockOutId INT;
    SELECT @maxStockOutId = ISNULL(MAX(stock_out_id), 0) FROM tbl_stock_out;
    
    DBCC CHECKIDENT ('tbl_stock_out', RESEED, @maxStockOutId);
    PRINT '✓ Fixed tbl_stock_out IDENTITY seed. Current max ID: ' + CAST(@maxStockOutId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_out table does not exist. Skipping...';
END
GO

-- ============================================
-- Fix tbl_stock_out_items IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out_items]') AND type in (N'U'))
BEGIN
    DECLARE @maxStockOutItemId INT;
    SELECT @maxStockOutItemId = ISNULL(MAX(stock_out_items_id), 0) FROM tbl_stock_out_items;
    
    DBCC CHECKIDENT ('tbl_stock_out_items', RESEED, @maxStockOutItemId);
    PRINT '✓ Fixed tbl_stock_out_items IDENTITY seed. Current max ID: ' + CAST(@maxStockOutItemId AS NVARCHAR(10));
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_out_items table does not exist. Skipping...';
END
GO

PRINT '';
PRINT '=== All IDENTITY Seeds Fixed! ===';
PRINT 'New records will now continue sequentially from the maximum existing ID in each table.';
PRINT '';
PRINT 'NOTE: If you have records with IDs >= 1000 that are test data,';
PRINT '      consider running FixHighIdRecords.sql to delete them first,';
PRINT '      then run this script again to reset the seeds properly.';
GO

