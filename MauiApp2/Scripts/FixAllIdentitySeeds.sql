-- Fix IDENTITY Seed for All Tables
-- This script resets the IDENTITY seed for all tables to continue from the maximum existing ID
-- Run this script if you notice IDs jumping to 1000 or other large numbers

-- ============================================
-- Fix tbl_roles IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_roles]') AND type in (N'U'))
BEGIN
    DECLARE @maxRoleId INT;
    SELECT @maxRoleId = ISNULL(MAX(role_id), 0) FROM tbl_roles;
    
    DBCC CHECKIDENT ('tbl_roles', RESEED, @maxRoleId);
    PRINT 'Fixed tbl_roles IDENTITY seed. Current max ID: ' + CAST(@maxRoleId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_users IDENTITY seed. Current max ID: ' + CAST(@maxUserId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_category IDENTITY seed. Current max ID: ' + CAST(@maxCategoryId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_brand IDENTITY seed. Current max ID: ' + CAST(@maxBrandId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_tax IDENTITY seed. Current max ID: ' + CAST(@maxTaxId AS NVARCHAR(10));
END
GO

-- ============================================
-- Fix tbl_product IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    DECLARE @maxProductId INT;
    SELECT @maxProductId = ISNULL(MAX(product_id), 0) FROM tbl_product;
    
    DBCC CHECKIDENT ('tbl_product', RESEED, @maxProductId);
    PRINT 'Fixed tbl_product IDENTITY seed. Current max ID: ' + CAST(@maxProductId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_supplier IDENTITY seed. Current max ID: ' + CAST(@maxSupplierId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_purchase_order IDENTITY seed. Current max ID: ' + CAST(@maxPOId AS NVARCHAR(10));
END
GO

-- ============================================
-- Fix tbl_purchase_order_items IDENTITY
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
BEGIN
    DECLARE @maxPOItemId INT = 0;
    
    -- Try po_item_id first (singular - most common)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'po_item_id')
    BEGIN
        SELECT @maxPOItemId = ISNULL(MAX(po_item_id), 0) FROM tbl_purchase_order_items;
        DBCC CHECKIDENT ('tbl_purchase_order_items', RESEED, @maxPOItemId);
        PRINT 'Fixed tbl_purchase_order_items IDENTITY seed (po_item_id). Current max ID: ' + CAST(@maxPOItemId AS NVARCHAR(10));
    END
    -- Try po_items_id (plural - alternative naming)
    ELSE IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'po_items_id')
    BEGIN
        SELECT @maxPOItemId = ISNULL(MAX(po_items_id), 0) FROM tbl_purchase_order_items;
        DBCC CHECKIDENT ('tbl_purchase_order_items', RESEED, @maxPOItemId);
        PRINT 'Fixed tbl_purchase_order_items IDENTITY seed (po_items_id). Current max ID: ' + CAST(@maxPOItemId AS NVARCHAR(10));
    END
    ELSE
    BEGIN
        PRINT 'WARNING: Could not find identity column (po_item_id or po_items_id) in tbl_purchase_order_items. Skipping...';
    END
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
    PRINT 'Fixed tbl_stock_in IDENTITY seed. Current max ID: ' + CAST(@maxStockInId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_stock_in_items IDENTITY seed. Current max ID: ' + CAST(@maxStockInItemId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_sales_order IDENTITY seed. Current max ID: ' + CAST(@maxSalesOrderId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_sales_order_items IDENTITY seed. Current max ID: ' + CAST(@maxSalesOrderItemId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_stock_out IDENTITY seed. Current max ID: ' + CAST(@maxStockOutId AS NVARCHAR(10));
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
    PRINT 'Fixed tbl_stock_out_items IDENTITY seed. Current max ID: ' + CAST(@maxStockOutItemId AS NVARCHAR(10));
END
GO

PRINT '';
PRINT '=== All IDENTITY seeds have been reset! ===';
PRINT 'New records will now continue from the maximum existing ID in each table.';
GO

