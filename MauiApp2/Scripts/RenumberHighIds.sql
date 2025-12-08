-- Renumber records with IDs >= 1000 to continue sequentially
-- This script will renumber records in tables that have IDs jumping to 1000+
-- It handles foreign key relationships properly

-- ============================================
-- Fix tbl_category: Renumber category_id >= 1000
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_category]') AND type in (N'U'))
BEGIN
    PRINT '=== Fixing tbl_category ===';
    
    -- Get the next available ID
    DECLARE @NextCategoryId INT;
    SELECT @NextCategoryId = ISNULL(MAX(category_id), 0) + 1 
    FROM tbl_category 
    WHERE category_id < 1000;
    
    PRINT 'Next available category_id: ' + CAST(@NextCategoryId AS NVARCHAR(10));
    
    -- Find all categories with IDs >= 1000
    DECLARE category_cursor CURSOR FOR
    SELECT category_id, category_name, description
    FROM tbl_category
    WHERE category_id >= 1000
    ORDER BY category_id;
    
    DECLARE @OldCategoryId INT, @CategoryName NVARCHAR(100), @Description NVARCHAR(MAX);
    DECLARE @NewCategoryId INT;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        OPEN category_cursor;
        FETCH NEXT FROM category_cursor INTO @OldCategoryId, @CategoryName, @Description;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @NewCategoryId = @NextCategoryId;
            
            -- Delete the old record
            DELETE FROM tbl_category WHERE category_id = @OldCategoryId;
            
            -- Insert with new ID
            SET IDENTITY_INSERT tbl_category ON;
            INSERT INTO tbl_category (category_id, category_name, description)
            VALUES (@NewCategoryId, @CategoryName, @Description);
            SET IDENTITY_INSERT tbl_category OFF;
            
            -- Update all foreign key references in tbl_product
            UPDATE tbl_product
            SET category_id = @NewCategoryId
            WHERE category_id = @OldCategoryId;
            
            PRINT 'Renumbered category ' + @CategoryName + ' from ' + CAST(@OldCategoryId AS NVARCHAR(10)) + ' to ' + CAST(@NewCategoryId AS NVARCHAR(10));
            
            SET @NextCategoryId = @NextCategoryId + 1;
            FETCH NEXT FROM category_cursor INTO @OldCategoryId, @CategoryName, @Description;
        END
        
        CLOSE category_cursor;
        DEALLOCATE category_cursor;
        
        -- Reset identity seed
        DECLARE @MaxCategoryId INT;
        SELECT @MaxCategoryId = ISNULL(MAX(category_id), 0) FROM tbl_category;
        DBCC CHECKIDENT ('tbl_category', RESEED, @MaxCategoryId);
        
        COMMIT TRANSACTION;
        PRINT 'tbl_category renumbering completed!';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        IF CURSOR_STATUS('global', 'category_cursor') >= 0
        BEGIN
            CLOSE category_cursor;
            DEALLOCATE category_cursor;
        END
        PRINT 'ERROR in tbl_category: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- ============================================
-- Fix tbl_brand: Renumber brand_id >= 1000
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
BEGIN
    PRINT '';
    PRINT '=== Fixing tbl_brand ===';
    
    DECLARE @NextBrandId INT;
    SELECT @NextBrandId = ISNULL(MAX(brand_id), 0) + 1 
    FROM tbl_brand 
    WHERE brand_id < 1000;
    
    PRINT 'Next available brand_id: ' + CAST(@NextBrandId AS NVARCHAR(10));
    
    DECLARE brand_cursor CURSOR FOR
    SELECT brand_id, brand_name, description
    FROM tbl_brand
    WHERE brand_id >= 1000
    ORDER BY brand_id;
    
    DECLARE @OldBrandId INT, @BrandName NVARCHAR(100), @BrandDescription NVARCHAR(MAX);
    DECLARE @NewBrandId INT;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        OPEN brand_cursor;
        FETCH NEXT FROM brand_cursor INTO @OldBrandId, @BrandName, @BrandDescription;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @NewBrandId = @NextBrandId;
            
            DELETE FROM tbl_brand WHERE brand_id = @OldBrandId;
            
            SET IDENTITY_INSERT tbl_brand ON;
            INSERT INTO tbl_brand (brand_id, brand_name, description)
            VALUES (@NewBrandId, @BrandName, @BrandDescription);
            SET IDENTITY_INSERT tbl_brand OFF;
            
            -- Update foreign keys in tbl_product
            UPDATE tbl_product
            SET brand_id = @NewBrandId
            WHERE brand_id = @OldBrandId;
            
            PRINT 'Renumbered brand ' + @BrandName + ' from ' + CAST(@OldBrandId AS NVARCHAR(10)) + ' to ' + CAST(@NewBrandId AS NVARCHAR(10));
            
            SET @NextBrandId = @NextBrandId + 1;
            FETCH NEXT FROM brand_cursor INTO @OldBrandId, @BrandName, @BrandDescription;
        END
        
        CLOSE brand_cursor;
        DEALLOCATE brand_cursor;
        
        DECLARE @MaxBrandId INT;
        SELECT @MaxBrandId = ISNULL(MAX(brand_id), 0) FROM tbl_brand;
        DBCC CHECKIDENT ('tbl_brand', RESEED, @MaxBrandId);
        
        COMMIT TRANSACTION;
        PRINT 'tbl_brand renumbering completed!';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        IF CURSOR_STATUS('global', 'brand_cursor') >= 0
        BEGIN
            CLOSE brand_cursor;
            DEALLOCATE brand_cursor;
        END
        PRINT 'ERROR in tbl_brand: ' + ERROR_MESSAGE();
    END CATCH
END
GO

-- ============================================
-- Fix tbl_product: Renumber product_id >= 1000
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    PRINT '';
    PRINT '=== Fixing tbl_product ===';
    
    DECLARE @NextProductId INT;
    SELECT @NextProductId = ISNULL(MAX(product_id), 0) + 1 
    FROM tbl_product 
    WHERE product_id < 1000;
    
    PRINT 'Next available product_id: ' + CAST(@NextProductId AS NVARCHAR(10));
    
    -- Get all product columns
    DECLARE @ProductColumns NVARCHAR(MAX) = '';
    SELECT @ProductColumns = STRING_AGG(COLUMN_NAME, ', ')
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'tbl_product' AND COLUMN_NAME != 'product_id';
    
    DECLARE product_cursor CURSOR FOR
    SELECT product_id, brand_id, category_id, tax_id, product_name, product_sku, 
           model_number, cost_price, sell_price, quantity, status, 
           created_date, modified_date
    FROM tbl_product
    WHERE product_id >= 1000
    ORDER BY product_id;
    
    DECLARE @OldProductId INT, @BrandId INT, @CategoryId INT, @TaxId INT;
    DECLARE @ProductName NVARCHAR(200), @ProductSku NVARCHAR(50), @ModelNumber NVARCHAR(100);
    DECLARE @CostPrice DECIMAL(18,2), @SellPrice DECIMAL(18,2), @Quantity INT;
    DECLARE @Status BIT, @CreatedDate DATETIME, @ModifiedDate DATETIME;
    DECLARE @NewProductId INT;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        OPEN product_cursor;
        FETCH NEXT FROM product_cursor INTO @OldProductId, @BrandId, @CategoryId, @TaxId,
            @ProductName, @ProductSku, @ModelNumber, @CostPrice, @SellPrice, 
            @Quantity, @Status, @CreatedDate, @ModifiedDate;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @NewProductId = @NextProductId;
            
            -- Delete old product
            DELETE FROM tbl_product WHERE product_id = @OldProductId;
            
            -- Insert with new ID
            SET IDENTITY_INSERT tbl_product ON;
            INSERT INTO tbl_product (
                product_id, brand_id, category_id, tax_id, product_name, product_sku,
                model_number, cost_price, sell_price, quantity, status,
                created_date, modified_date
            )
            VALUES (
                @NewProductId, @BrandId, @CategoryId, @TaxId, @ProductName, @ProductSku,
                @ModelNumber, @CostPrice, @SellPrice, @Quantity, @Status,
                @CreatedDate, @ModifiedDate
            );
            SET IDENTITY_INSERT tbl_product OFF;
            
            -- Update foreign keys in related tables
            -- Purchase Order Items
            IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
            BEGIN
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'product_id')
                BEGIN
                    UPDATE tbl_purchase_order_items
                    SET product_id = @NewProductId
                    WHERE product_id = @OldProductId;
                END
            END
            
            -- Stock In Items
            IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
            BEGIN
                UPDATE tbl_stock_in_items
                SET product_id = @NewProductId
                WHERE product_id = @OldProductId;
            END
            
            -- Sales Order Items
            IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
            BEGIN
                UPDATE tbl_sales_order_items
                SET product_id = @NewProductId
                WHERE product_id = @OldProductId;
            END
            
            -- Stock Out Items
            IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out_items]') AND type in (N'U'))
            BEGIN
                UPDATE tbl_stock_out_items
                SET product_id = @NewProductId
                WHERE product_id = @OldProductId;
            END
            
            PRINT 'Renumbered product ' + @ProductName + ' from ' + CAST(@OldProductId AS NVARCHAR(10)) + ' to ' + CAST(@NewProductId AS NVARCHAR(10));
            
            SET @NextProductId = @NextProductId + 1;
            FETCH NEXT FROM product_cursor INTO @OldProductId, @BrandId, @CategoryId, @TaxId,
                @ProductName, @ProductSku, @ModelNumber, @CostPrice, @SellPrice, 
                @Quantity, @Status, @CreatedDate, @ModifiedDate;
        END
        
        CLOSE product_cursor;
        DEALLOCATE product_cursor;
        
        DECLARE @MaxProductId INT;
        SELECT @MaxProductId = ISNULL(MAX(product_id), 0) FROM tbl_product;
        DBCC CHECKIDENT ('tbl_product', RESEED, @MaxProductId);
        
        COMMIT TRANSACTION;
        PRINT 'tbl_product renumbering completed!';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        IF CURSOR_STATUS('global', 'product_cursor') >= 0
        BEGIN
            CLOSE product_cursor;
            DEALLOCATE product_cursor;
        END
        PRINT 'ERROR in tbl_product: ' + ERROR_MESSAGE();
        PRINT 'Error Line: ' + CAST(ERROR_LINE() AS NVARCHAR(10));
    END CATCH
END
GO

PRINT '';
PRINT '=== Renumbering Complete! ===';
PRINT 'All records with IDs >= 1000 have been renumbered to continue sequentially.';
PRINT 'Identity seeds have been reset.';
GO

