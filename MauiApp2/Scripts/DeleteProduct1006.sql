-- Delete Product 1006 (Test Data)
-- This script deletes product_id 1006 and all related records
-- Use this ONLY if product 1006 is test data that you want to remove
-- 
-- WARNING: This will permanently delete the product and all related records!
-- Make sure to backup your database before running this script.

PRINT '=== Deleting Product 1006 and Related Records ===';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY
    -- Check if product 1006 exists
    IF EXISTS (SELECT * FROM tbl_product WHERE product_id = 1006)
    BEGIN
        DECLARE @productName NVARCHAR(255);
        SELECT @productName = product_name FROM tbl_product WHERE product_id = 1006;
        
        PRINT 'Found product 1006: ' + ISNULL(@productName, 'Unknown');
        PRINT '';
        PRINT 'Deleting related records...';
        
        -- Delete from purchase order items
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
        BEGIN
            DECLARE @poItemsCount INT;
            SELECT @poItemsCount = COUNT(*) FROM tbl_purchase_order_items WHERE product_id = 1006;
            IF @poItemsCount > 0
            BEGIN
                DELETE FROM tbl_purchase_order_items WHERE product_id = 1006;
                PRINT '  ✓ Deleted ' + CAST(@poItemsCount AS NVARCHAR(10)) + ' purchase order item(s)';
            END
        END
        
        -- Delete from stock in items
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
        BEGIN
            DECLARE @stockInItemsCount INT;
            SELECT @stockInItemsCount = COUNT(*) FROM tbl_stock_in_items WHERE product_id = 1006;
            IF @stockInItemsCount > 0
            BEGIN
                DELETE FROM tbl_stock_in_items WHERE product_id = 1006;
                PRINT '  ✓ Deleted ' + CAST(@stockInItemsCount AS NVARCHAR(10)) + ' stock in item(s)';
            END
        END
        
        -- Delete from sales order items
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
        BEGIN
            DECLARE @salesItemsCount INT;
            SELECT @salesItemsCount = COUNT(*) FROM tbl_sales_order_items WHERE product_id = 1006;
            IF @salesItemsCount > 0
            BEGIN
                DELETE FROM tbl_sales_order_items WHERE product_id = 1006;
                PRINT '  ✓ Deleted ' + CAST(@salesItemsCount AS NVARCHAR(10)) + ' sales order item(s)';
            END
        END
        
        -- Delete from stock out items
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out_items]') AND type in (N'U'))
        BEGIN
            DECLARE @stockOutItemsCount INT;
            SELECT @stockOutItemsCount = COUNT(*) FROM tbl_stock_out_items WHERE product_id = 1006;
            IF @stockOutItemsCount > 0
            BEGIN
                DELETE FROM tbl_stock_out_items WHERE product_id = 1006;
                PRINT '  ✓ Deleted ' + CAST(@stockOutItemsCount AS NVARCHAR(10)) + ' stock out item(s)';
            END
        END
        
        -- Delete the product
        DELETE FROM tbl_product WHERE product_id = 1006;
        PRINT '';
        PRINT '  ✓ Deleted product 1006';
        PRINT '';
        
        -- Reset identity seed
        DECLARE @maxProductId INT;
        SELECT @maxProductId = ISNULL(MAX(product_id), 0) FROM tbl_product;
        DBCC CHECKIDENT ('tbl_product', RESEED, @maxProductId);
        PRINT '✓ Reset product IDENTITY seed. Next product ID will be: ' + CAST(@maxProductId + 1 AS NVARCHAR(10));
        
        COMMIT TRANSACTION;
        
        PRINT '';
        PRINT '=== Product 1006 Deleted Successfully! ===';
        PRINT 'Next product will now continue from ID: ' + CAST(@maxProductId + 1 AS NVARCHAR(10));
    END
    ELSE
    BEGIN
        PRINT '⚠ Product 1006 does not exist. Nothing to delete.';
        COMMIT TRANSACTION;
    END
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '❌ Error occurred. Transaction rolled back.';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
END CATCH;
GO





