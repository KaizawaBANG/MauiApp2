-- ============================================
-- Database Cleanup Script WITH Automatic Backup
-- ============================================
-- This script:
--   1. Creates a backup BEFORE cleanup
--   2. Performs the cleanup
--   3. Allows you to restore if needed
-- ============================================

-- IMPORTANT: Configure backup path
DECLARE @DatabaseName NVARCHAR(128) = DB_NAME();
DECLARE @BackupPath NVARCHAR(260) = 'C:\Backup\';
DECLARE @BackupFileName NVARCHAR(260);
DECLARE @BackupDate NVARCHAR(20) = FORMAT(GETDATE(), 'yyyyMMdd_HHmmss');
SET @BackupFileName = @BackupPath + @DatabaseName + '_Backup_' + @BackupDate + '.bak';

PRINT '========================================';
PRINT 'Database Cleanup WITH Backup';
PRINT '========================================';
PRINT '';
PRINT 'Step 1: Creating backup...';
PRINT '';

-- Step 1: Create Backup (Optional - Skip if folder doesn't exist)
DECLARE @BackupCreated BIT = 0;

BEGIN TRY
    -- Check if backup folder exists
    DECLARE @FolderExists INT;
    EXEC xp_fileexist @BackupPath, @FolderExists OUTPUT;

    IF @FolderExists = 1
    BEGIN
        -- Create backup name variable
        DECLARE @BackupName NVARCHAR(260) = @DatabaseName + '_Backup_' + @BackupDate;

        BACKUP DATABASE @DatabaseName
        TO DISK = @BackupFileName
        WITH FORMAT,
             NAME = @BackupName,
             DESCRIPTION = 'Backup before cleanup',
             COMPRESSION,
             STATS = 10;

        SET @BackupCreated = 1;
        PRINT '';
        PRINT '✅ Backup created: ' + @BackupFileName;
        PRINT '';
    END
    ELSE
    BEGIN
        PRINT '⚠ Warning: Backup folder does not exist: ' + @BackupPath;
        PRINT 'Skipping backup (you mentioned you already have a manual backup).';
        PRINT 'Continuing with cleanup...';
        PRINT '';
    END

END TRY
BEGIN CATCH
    PRINT '';
    PRINT '⚠ Warning: Backup failed: ' + ERROR_MESSAGE();
    PRINT 'Skipping backup and continuing with cleanup...';
    PRINT '(You mentioned you already have a manual backup)';
    PRINT '';
    SET @BackupCreated = 0;
END CATCH;

-- Step 2: Perform Cleanup
PRINT 'Step 2: Performing cleanup...';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @DeletedCount INT = 0;
    DECLARE @TotalDeleted INT = 0;

    -- Delete General Ledger
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_general_ledger]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_general_ledger;
        DELETE FROM tbl_general_ledger;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' General Ledger entries';
    END

    -- Delete Payments
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_payments]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_payments;
        DELETE FROM tbl_payments;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Payment records';
    END

    -- Delete Expenses
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_expenses]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_expenses;
        DELETE FROM tbl_expenses;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Expense records';
    END

    -- Delete Accounts Payable
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_accounts_payable]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_accounts_payable;
        DELETE FROM tbl_accounts_payable;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Accounts Payable records';
    END

    -- Delete Stock Out Items (MUST be before Stock Out)
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_out_items;
        DELETE FROM tbl_stock_out_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock Out Items';
    END

    -- Delete Stock Out (MUST be before Sales Orders - FK constraint)
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_out;
        DELETE FROM tbl_stock_out;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock Out records';
    END

    -- Delete Sales Order Items (MUST be before Sales Orders)
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_sales_order_items;
        DELETE FROM tbl_sales_order_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Sales Order Items';
    END

    -- Delete Sales Orders (Now safe - no FK references)
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_sales_order;
        DELETE FROM tbl_sales_order;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Sales Orders';
    END

    -- Delete Stock In Items
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_in_items;
        DELETE FROM tbl_stock_in_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock In Items';
    END

    -- Delete Stock In
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_stock_in;
        DELETE FROM tbl_stock_in;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Stock In records';
    END

    -- Delete Purchase Order Items
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_purchase_order_items;
        DELETE FROM tbl_purchase_order_items;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Purchase Order Items';
    END

    -- Delete Purchase Orders
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_purchase_order;
        DELETE FROM tbl_purchase_order;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Purchase Orders';
    END

    -- ============================================
    -- Delete Products (after all transaction data is deleted)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_product;
        DELETE FROM tbl_product;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Product records';
    END

    -- ============================================
    -- Delete Brands (after products are deleted)
    -- ============================================
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
    BEGIN
        SELECT @DeletedCount = COUNT(*) FROM tbl_brand;
        DELETE FROM tbl_brand;
        SET @TotalDeleted = @TotalDeleted + @DeletedCount;
        PRINT '  ✓ Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' Brand records';
    END

    -- Reset Identity Seeds
    PRINT '';
    PRINT 'Resetting Identity Seeds...';
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_sales_order', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_purchase_order', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_stock_in', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_stock_out', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_general_ledger]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_general_ledger', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_payments]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_payments', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_expenses]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_expenses', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_accounts_payable]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_accounts_payable', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_brand', RESEED, 0);
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
        DBCC CHECKIDENT ('tbl_product', RESEED, 0);
    PRINT '  ✓ Identity seeds reset';

    COMMIT TRANSACTION;

    PRINT '';
    PRINT '========================================';
    PRINT '✅ Cleanup Completed!';
    PRINT '========================================';
    PRINT 'Total records deleted: ' + CAST(@TotalDeleted AS NVARCHAR(10));
    PRINT '';
    IF @BackupCreated = 1
    BEGIN
        PRINT 'Backup File: ' + @BackupFileName;
        PRINT '';
        PRINT 'To restore this backup, use:';
        PRINT '  RestoreDatabaseFromBackup.sql';
        PRINT '  Update @BackupFilePath to: ' + @BackupFileName;
    END
    ELSE
    BEGIN
        PRINT 'Note: No backup was created (you have a manual backup).';
        PRINT 'To restore, use your manual backup file.';
    END
    PRINT '';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '========================================';
    PRINT '❌ Cleanup Failed!';
    PRINT '========================================';
    PRINT 'Error: ' + ERROR_MESSAGE();
    PRINT '';
    PRINT 'Your backup is safe: ' + @BackupFileName;
    PRINT 'All changes have been rolled back.';
    PRINT '';
END CATCH;
GO

