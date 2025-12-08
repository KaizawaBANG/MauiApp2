-- ============================================
-- Database Cleanup Script - Keep Essential Data
-- ============================================
-- This script removes transaction/test data while preserving:
--   ✓ Users and Roles
--   ✓ Chart of Accounts
--   ✓ Tax Settings
--   ✓ Master Data (Brands, Categories, Suppliers, Products)
--
-- Removes:
--   ✗ Sales Orders and Items
--   ✗ Purchase Orders and Items
--   ✗ Stock In/Out Records
--   ✗ Payments
--   ✗ Expenses
--   ✗ Accounts Payable
--   ✗ General Ledger Entries
--   ✗ Stock Adjustments
--
-- WARNING: This will permanently delete all transaction data!
-- Make sure to backup your database before running this script.
-- ============================================

PRINT '========================================';
PRINT 'Database Cleanup - Keep Essential Data';
PRINT '========================================';
PRINT '';
PRINT 'This script will:';
PRINT '  ✓ Keep: Users, Roles, Chart of Accounts, Tax, Master Data';
PRINT '  ✗ Delete: All transaction data (Sales, Purchases, Payments, etc.)';
PRINT '';
PRINT 'Press Ctrl+C to cancel, or wait 5 seconds to continue...';
PRINT '';

WAITFOR DELAY '00:00:05';

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @DeletedCount INT = 0;
    DECLARE @TotalDeleted INT = 0;

    PRINT 'Starting cleanup...';
    PRINT '';

    -- ============================================
    -- 1. Delete General Ledger Entries
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_general_ledger]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_general_ledger;
        DELETE FROM tbl_general_ledger;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' General Ledger entries';
    END

    -- ============================================
    -- 2. Delete Payments
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_payments]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_payments;
        DELETE FROM tbl_payments;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Payment records';
    END

    -- ============================================
    -- 3. Delete Expenses
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_expenses]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_expenses;
        DELETE FROM tbl_expenses;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Expense records';
    END

    -- ============================================
    -- 4. Delete Accounts Payable
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_accounts_payable]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_accounts_payable;
        DELETE FROM tbl_accounts_payable;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Accounts Payable records';
    END

    -- ============================================
    -- 5. Delete Stock Out Items (MUST be before Stock Out)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_out_items;
        DELETE FROM tbl_stock_out_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock Out Items';
    END

    -- ============================================
    -- 6. Delete Stock Out Records (MUST be before Sales Orders - FK constraint)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_out;
        DELETE FROM tbl_stock_out;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock Out records';
    END

    -- ============================================
    -- 7. Delete Sales Order Items (MUST be before Sales Orders)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_sales_order_items;
        DELETE FROM tbl_sales_order_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Sales Order Items';
    END

    -- ============================================
    -- 8. Delete Sales Orders (Now safe - no FK references)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_sales_order;
        DELETE FROM tbl_sales_order;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Sales Orders';
    END

    -- ============================================
    -- 9. Delete Stock In Items
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_in_items;
        DELETE FROM tbl_stock_in_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock In Items';
    END

    -- ============================================
    -- 10. Delete Stock In Records
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_in;
        DELETE FROM tbl_stock_in;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock In records';
    END

    -- ============================================
    -- 11. Delete Purchase Order Items
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_purchase_order_items;
        DELETE FROM tbl_purchase_order_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Purchase Order Items';
    END

    -- ============================================
    -- 12. Delete Purchase Orders
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_purchase_order;
        DELETE FROM tbl_purchase_order;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Purchase Orders';
    END

    -- ============================================
    -- 13. Delete Stock Adjustments (if exists)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_adjustment]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_adjustment;
        DELETE FROM tbl_stock_adjustment;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock Adjustments';
    END

    -- ============================================
    -- 14. Delete Products (after all transaction data is deleted)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_product;
        DELETE FROM tbl_product;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Product records';
    END

    -- ============================================
    -- 15. Delete Brands (after products are deleted)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_brand;
        DELETE FROM tbl_brand;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Brand records';
    END

    -- ============================================
    -- 15. Reset Identity Seeds
    -- ============================================
    PRINT '';
    PRINT 'Resetting Identity Seeds...';

    -- Reset Sales Order
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_sales_order', RESEED, 0);
        PRINT '  ✓ Reset tbl_sales_order IDENTITY seed';
    END

    -- Reset Purchase Order
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_purchase_order', RESEED, 0);
        PRINT '  ✓ Reset tbl_purchase_order IDENTITY seed';
    END

    -- Reset Stock In
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_stock_in', RESEED, 0);
        PRINT '  ✓ Reset tbl_stock_in IDENTITY seed';
    END

    -- Reset Stock Out
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_stock_out', RESEED, 0);
        PRINT '  ✓ Reset tbl_stock_out IDENTITY seed';
    END

    -- Reset General Ledger
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_general_ledger]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_general_ledger', RESEED, 0);
        PRINT '  ✓ Reset tbl_general_ledger IDENTITY seed';
    END

    -- Reset Payments
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_payments]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_payments', RESEED, 0);
        PRINT '  ✓ Reset tbl_payments IDENTITY seed';
    END

    -- Reset Expenses
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_expenses]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_expenses', RESEED, 0);
        PRINT '  ✓ Reset tbl_expenses IDENTITY seed';
    END

    -- Reset Accounts Payable
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_accounts_payable]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_accounts_payable', RESEED, 0);
        PRINT '  ✓ Reset tbl_accounts_payable IDENTITY seed';
    END

    -- Reset Brand
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_brand', RESEED, 0);
        PRINT '  ✓ Reset tbl_brand IDENTITY seed';
    END

    -- Reset Product
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
    BEGIN
        DBCC CHECKIDENT ('tbl_product', RESEED, 0);
        PRINT '  ✓ Reset tbl_product IDENTITY seed';
    END

    -- Commit transaction
    COMMIT TRANSACTION;

    PRINT '';
    PRINT '========================================';
    PRINT '✅ Database Cleanup Completed!';
    PRINT '========================================';
    PRINT 'Total records deleted: ' + CAST(@TotalDeleted AS NVARCHAR(10));
    PRINT '';
    PRINT 'Kept Essential Data:';
    PRINT '  ✓ Users and Roles';
    PRINT '  ✓ Chart of Accounts';
    PRINT '  ✓ Tax Settings';
    PRINT '  ✓ Categories';
    PRINT '  ✓ Suppliers';
    PRINT '';
    PRINT 'Deleted Master Data:';
    PRINT '  ✗ Products';
    PRINT '  ✗ Brands';
    PRINT '';
    PRINT 'All transaction data has been removed.';
    PRINT 'Identity seeds have been reset.';
    PRINT 'Product quantities have been reset to 0.';
    PRINT '';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '========================================';
    PRINT '❌ ERROR: Cleanup Failed!';
    PRINT '========================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '';
    PRINT 'All changes have been rolled back.';
    PRINT 'Your database is unchanged.';
    PRINT '';
END CATCH;
GO

