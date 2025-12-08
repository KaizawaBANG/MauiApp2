-- Fix High ID Records (1000+)
-- This script will renumber or delete records with IDs >= 1000
-- Option 1: Delete high ID records (recommended if they're test data)
-- Option 2: Renumber them to continue sequentially

-- ============================================
-- Option 1: DELETE High ID Records
-- ============================================

BEGIN TRANSACTION;

BEGIN TRY
    PRINT '=== Fixing High ID Records (1000+) ===';
    PRINT '';

    -- ============================================
    -- Fix tbl_category
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_category]') AND type in (N'U'))
    BEGIN
        DECLARE @categoryCount INT;
        SELECT @categoryCount = COUNT(*) FROM tbl_category WHERE category_id >= 1000;
        
        IF @categoryCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@categoryCount AS NVARCHAR(10)) + ' category record(s) with ID >= 1000...';
            
            -- Check for foreign key references
            IF EXISTS (SELECT * FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID('tbl_category'))
            BEGIN
                -- Delete products first that reference these categories
                DELETE FROM tbl_product WHERE category_id >= 1000;
                PRINT 'Deleted products referencing high ID categories';
            END
            
            DELETE FROM tbl_category WHERE category_id >= 1000;
            PRINT 'Deleted high ID categories';
        END
        
        -- Reset identity seed
        DECLARE @maxCategoryId INT;
        SELECT @maxCategoryId = ISNULL(MAX(category_id), 0) FROM tbl_category;
        DBCC CHECKIDENT ('tbl_category', RESEED, @maxCategoryId);
        PRINT 'Fixed tbl_category IDENTITY seed. Max ID: ' + CAST(@maxCategoryId AS NVARCHAR(10));
    END

    -- ============================================
    -- Fix tbl_brand
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
    BEGIN
        DECLARE @brandCount INT;
        SELECT @brandCount = COUNT(*) FROM tbl_brand WHERE brand_id >= 1000;
        
        IF @brandCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@brandCount AS NVARCHAR(10)) + ' brand record(s) with ID >= 1000...';
            
            -- Check for foreign key references
            IF EXISTS (SELECT * FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID('tbl_brand'))
            BEGIN
                -- Delete products first that reference these brands
                DELETE FROM tbl_product WHERE brand_id >= 1000;
                PRINT 'Deleted products referencing high ID brands';
            END
            
            DELETE FROM tbl_brand WHERE brand_id >= 1000;
            PRINT 'Deleted high ID brands';
        END
        
        -- Reset identity seed
        DECLARE @maxBrandId INT;
        SELECT @maxBrandId = ISNULL(MAX(brand_id), 0) FROM tbl_brand;
        DBCC CHECKIDENT ('tbl_brand', RESEED, @maxBrandId);
        PRINT 'Fixed tbl_brand IDENTITY seed. Max ID: ' + CAST(@maxBrandId AS NVARCHAR(10));
    END

    -- ============================================
    -- Fix tbl_product
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
    BEGIN
        DECLARE @productCount INT;
        SELECT @productCount = COUNT(*) FROM tbl_product WHERE product_id >= 1000;
        
        IF @productCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@productCount AS NVARCHAR(10)) + ' product record(s) with ID >= 1000...';
            
            -- Delete related records first (foreign key dependencies)
            DELETE FROM tbl_purchase_order_items WHERE product_id IN (SELECT product_id FROM tbl_product WHERE product_id >= 1000);
            DELETE FROM tbl_stock_in_items WHERE product_id IN (SELECT product_id FROM tbl_product WHERE product_id >= 1000);
            DELETE FROM tbl_sales_order_items WHERE product_id IN (SELECT product_id FROM tbl_product WHERE product_id >= 1000);
            DELETE FROM tbl_stock_out_items WHERE product_id IN (SELECT product_id FROM tbl_product WHERE product_id >= 1000);
            
            DELETE FROM tbl_product WHERE product_id >= 1000;
            PRINT 'Deleted high ID products and related records';
        END
        
        -- Reset identity seed
        DECLARE @maxProductId INT;
        SELECT @maxProductId = ISNULL(MAX(product_id), 0) FROM tbl_product;
        DBCC CHECKIDENT ('tbl_product', RESEED, @maxProductId);
        PRINT 'Fixed tbl_product IDENTITY seed. Max ID: ' + CAST(@maxProductId AS NVARCHAR(10));
    END

    -- ============================================
    -- Fix tbl_tax (if any high IDs)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_tax]') AND type in (N'U'))
    BEGIN
        DECLARE @taxCount INT;
        SELECT @taxCount = COUNT(*) FROM tbl_tax WHERE tax_id >= 1000;
        
        IF @taxCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@taxCount AS NVARCHAR(10)) + ' tax record(s) with ID >= 1000...';
            
            -- Update products to use default tax before deleting
            UPDATE tbl_product SET tax_id = (SELECT MIN(tax_id) FROM tbl_tax WHERE tax_id < 1000) 
            WHERE tax_id >= 1000;
            
            DELETE FROM tbl_tax WHERE tax_id >= 1000;
            PRINT 'Deleted high ID taxes';
        END
        
        -- Reset identity seed
        DECLARE @maxTaxId INT;
        SELECT @maxTaxId = ISNULL(MAX(tax_id), 0) FROM tbl_tax;
        DBCC CHECKIDENT ('tbl_tax', RESEED, @maxTaxId);
        PRINT 'Fixed tbl_tax IDENTITY seed. Max ID: ' + CAST(@maxTaxId AS NVARCHAR(10));
    END

    -- ============================================
    -- Fix tbl_supplier (if any high IDs)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND type in (N'U'))
    BEGIN
        DECLARE @supplierCount INT;
        SELECT @supplierCount = COUNT(*) FROM tbl_supplier WHERE supplier_id >= 1000;
        
        IF @supplierCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@supplierCount AS NVARCHAR(10)) + ' supplier record(s) with ID >= 1000...';
            
            -- Update purchase orders to NULL or delete them
            UPDATE tbl_purchase_order SET supplier_id = NULL WHERE supplier_id >= 1000;
            UPDATE tbl_stock_in SET supplier_id = NULL WHERE supplier_id >= 1000;
            
            DELETE FROM tbl_supplier WHERE supplier_id >= 1000;
            PRINT 'Deleted high ID suppliers';
        END
        
        -- Reset identity seed
        DECLARE @maxSupplierId INT;
        SELECT @maxSupplierId = ISNULL(MAX(supplier_id), 0) FROM tbl_supplier;
        DBCC CHECKIDENT ('tbl_supplier', RESEED, @maxSupplierId);
        PRINT 'Fixed tbl_supplier IDENTITY seed. Max ID: ' + CAST(@maxSupplierId AS NVARCHAR(10));
    END

    -- ============================================
    -- Fix other tables with high IDs
    -- ============================================
    
    -- tbl_purchase_order
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND type in (N'U'))
    BEGIN
        DECLARE @poCount INT;
        SELECT @poCount = COUNT(*) FROM tbl_purchase_order WHERE po_id >= 1000;
        
        IF @poCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@poCount AS NVARCHAR(10)) + ' purchase order(s) with ID >= 1000...';
            DELETE FROM tbl_purchase_order_items WHERE po_id IN (SELECT po_id FROM tbl_purchase_order WHERE po_id >= 1000);
            DELETE FROM tbl_stock_in WHERE po_id IN (SELECT po_id FROM tbl_purchase_order WHERE po_id >= 1000);
            DELETE FROM tbl_purchase_order WHERE po_id >= 1000;
            PRINT 'Deleted high ID purchase orders';
        END
        
        DECLARE @maxPOId INT;
        SELECT @maxPOId = ISNULL(MAX(po_id), 0) FROM tbl_purchase_order;
        DBCC CHECKIDENT ('tbl_purchase_order', RESEED, @maxPOId);
        PRINT 'Fixed tbl_purchase_order IDENTITY seed. Max ID: ' + CAST(@maxPOId AS NVARCHAR(10));
    END

    -- tbl_stock_in
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
    BEGIN
        DECLARE @stockInCount INT;
        SELECT @stockInCount = COUNT(*) FROM tbl_stock_in WHERE stock_in_id >= 1000;
        
        IF @stockInCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@stockInCount AS NVARCHAR(10)) + ' stock in record(s) with ID >= 1000...';
            DELETE FROM tbl_stock_in_items WHERE stock_in_id IN (SELECT stock_in_id FROM tbl_stock_in WHERE stock_in_id >= 1000);
            DELETE FROM tbl_stock_in WHERE stock_in_id >= 1000;
            PRINT 'Deleted high ID stock in records';
        END
        
        DECLARE @maxStockInId INT;
        SELECT @maxStockInId = ISNULL(MAX(stock_in_id), 0) FROM tbl_stock_in;
        DBCC CHECKIDENT ('tbl_stock_in', RESEED, @maxStockInId);
        PRINT 'Fixed tbl_stock_in IDENTITY seed. Max ID: ' + CAST(@maxStockInId AS NVARCHAR(10));
    END

    -- tbl_sales_order
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
    BEGIN
        DECLARE @salesOrderCount INT;
        SELECT @salesOrderCount = COUNT(*) FROM tbl_sales_order WHERE sales_order_id >= 1000;
        
        IF @salesOrderCount > 0
        BEGIN
            PRINT 'Deleting ' + CAST(@salesOrderCount AS NVARCHAR(10)) + ' sales order(s) with ID >= 1000...';
            DELETE FROM tbl_sales_order_items WHERE sales_order_id IN (SELECT sales_order_id FROM tbl_sales_order WHERE sales_order_id >= 1000);
            DELETE FROM tbl_sales_order WHERE sales_order_id >= 1000;
            PRINT 'Deleted high ID sales orders';
        END
        
        DECLARE @maxSalesOrderId INT;
        SELECT @maxSalesOrderId = ISNULL(MAX(sales_order_id), 0) FROM tbl_sales_order;
        DBCC CHECKIDENT ('tbl_sales_order', RESEED, @maxSalesOrderId);
        PRINT 'Fixed tbl_sales_order IDENTITY seed. Max ID: ' + CAST(@maxSalesOrderId AS NVARCHAR(10));
    END

    COMMIT TRANSACTION;
    
    PRINT '';
    PRINT '=== All High ID Records Fixed! ===';
    PRINT 'All records with ID >= 1000 have been deleted.';
    PRINT 'Identity seeds have been reset to continue sequentially.';
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '❌ Error occurred. Transaction rolled back.';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
END CATCH;
GO







