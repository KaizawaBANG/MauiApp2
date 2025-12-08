-- Fix Cloud Database Tables to Match Local Database Structure
-- Run this script on your cloud database to add missing columns
-- This fixes the sync errors you encountered

PRINT '=== Fixing Cloud Database Tables to Match Local Structure ===';
PRINT '';

-- ============================================
-- Fix tbl_stock_in - Add missing columns
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
BEGIN
    -- Add stock_in_date if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND name = 'stock_in_date')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in]
        ADD [stock_in_date] DATETIME NOT NULL DEFAULT GETDATE();
        PRINT '✓ Added stock_in_date to tbl_stock_in';
    END

    -- Add processed_by if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND name = 'processed_by')
    BEGIN
        -- Add as nullable first
        ALTER TABLE [dbo].[tbl_stock_in]
        ADD [processed_by] INT NULL;
        
        -- Set default value for existing rows (use first user or 1)
        DECLARE @defaultUserId INT;
        SELECT @defaultUserId = ISNULL(MIN(user_id), 1) FROM tbl_users;
        IF @defaultUserId IS NULL SET @defaultUserId = 1;
        
        UPDATE [dbo].[tbl_stock_in] SET [processed_by] = @defaultUserId;
        
        -- Now make it NOT NULL
        ALTER TABLE [dbo].[tbl_stock_in]
        ALTER COLUMN [processed_by] INT NOT NULL;
        
        -- Add foreign key constraint
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_users]') AND type in (N'U'))
        BEGIN
            ALTER TABLE [dbo].[tbl_stock_in]
            ADD CONSTRAINT [FK_stock_in_user] FOREIGN KEY ([processed_by]) REFERENCES [dbo].[tbl_users]([user_id]);
        END
        
        PRINT '✓ Added processed_by to tbl_stock_in';
    END
END
GO

-- ============================================
-- Fix tbl_stock_in_items - Add rejected columns
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
BEGIN
    -- Add quantity_rejected if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND name = 'quantity_rejected')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in_items]
        ADD [quantity_rejected] INT NOT NULL DEFAULT 0;
        
        IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_quantity_rejected')
        BEGIN
            ALTER TABLE [dbo].[tbl_stock_in_items]
            ADD CONSTRAINT [CK_quantity_rejected] CHECK ([quantity_rejected] >= 0);
        END
        
        PRINT '✓ Added quantity_rejected to tbl_stock_in_items';
    END

    -- Add rejection_reason if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND name = 'rejection_reason')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in_items]
        ADD [rejection_reason] NVARCHAR(100) NULL;
        PRINT '✓ Added rejection_reason to tbl_stock_in_items';
    END

    -- Add rejection_remarks if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND name = 'rejection_remarks')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_in_items]
        ADD [rejection_remarks] NVARCHAR(MAX) NULL;
        PRINT '✓ Added rejection_remarks to tbl_stock_in_items';
    END
END
GO

-- ============================================
-- Fix tbl_sales_order - Add missing columns
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
BEGIN
    -- Add sales_order_number if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND name = 'sales_order_number')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order]
        ADD [sales_order_number] NVARCHAR(50) NULL;
        
        -- Update existing rows with generated numbers
        DECLARE @counter INT = 1;
        DECLARE @soId INT;
        DECLARE cur CURSOR FOR SELECT sales_order_id FROM tbl_sales_order WHERE sales_order_number IS NULL;
        OPEN cur;
        FETCH NEXT FROM cur INTO @soId;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            UPDATE tbl_sales_order SET sales_order_number = 'INV-' + RIGHT('000' + CAST(@counter AS VARCHAR), 3) WHERE sales_order_id = @soId;
            SET @counter = @counter + 1;
            FETCH NEXT FROM cur INTO @soId;
        END
        CLOSE cur;
        DEALLOCATE cur;
        
        -- Make NOT NULL after updating
        ALTER TABLE [dbo].[tbl_sales_order]
        ALTER COLUMN [sales_order_number] NVARCHAR(50) NOT NULL;
        
        -- Add unique constraint
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_sales_order_number')
        BEGIN
            CREATE UNIQUE INDEX [IX_sales_order_number] ON [dbo].[tbl_sales_order] ([sales_order_number]);
        END
        
        PRINT '✓ Added sales_order_number to tbl_sales_order';
    END

    -- Add sales_date if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND name = 'sales_date')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order]
        ADD [sales_date] DATETIME NOT NULL DEFAULT GETDATE();
        PRINT '✓ Added sales_date to tbl_sales_order';
    END

    -- Add subtotal if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND name = 'subtotal')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order]
        ADD [subtotal] DECIMAL(18,2) NOT NULL DEFAULT 0;
        PRINT '✓ Added subtotal to tbl_sales_order';
    END

    -- Add tax_amount if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND name = 'tax_amount')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order]
        ADD [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0;
        PRINT '✓ Added tax_amount to tbl_sales_order';
    END

    -- Add payment_method if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND name = 'payment_method')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order]
        ADD [payment_method] NVARCHAR(50) NOT NULL DEFAULT 'Cash';
        PRINT '✓ Added payment_method to tbl_sales_order';
    END

    -- Add processed_by if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND name = 'processed_by')
    BEGIN
        -- Add as nullable first
        ALTER TABLE [dbo].[tbl_sales_order]
        ADD [processed_by] INT NULL;
        
        -- Set default value for existing rows
        DECLARE @defaultUserId2 INT;
        SELECT @defaultUserId2 = ISNULL(MIN(user_id), 1) FROM tbl_users;
        IF @defaultUserId2 IS NULL SET @defaultUserId2 = 1;
        
        UPDATE [dbo].[tbl_sales_order] SET [processed_by] = @defaultUserId2;
        
        -- Now make it NOT NULL
        ALTER TABLE [dbo].[tbl_sales_order]
        ALTER COLUMN [processed_by] INT NOT NULL;
        
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_users]') AND type in (N'U'))
        BEGIN
            ALTER TABLE [dbo].[tbl_sales_order]
            ADD CONSTRAINT [FK_sales_order_user] FOREIGN KEY ([processed_by]) REFERENCES [dbo].[tbl_users]([user_id]);
        END
        
        PRINT '✓ Added processed_by to tbl_sales_order';
    END
END
GO

-- ============================================
-- Fix tbl_sales_order_items - Add missing columns
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
BEGIN
    -- Add tax_rate if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND name = 'tax_rate')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order_items]
        ADD [tax_rate] DECIMAL(5,2) NOT NULL DEFAULT 0;
        PRINT '✓ Added tax_rate to tbl_sales_order_items';
    END

    -- Add tax_amount if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND name = 'tax_amount')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order_items]
        ADD [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0;
        PRINT '✓ Added tax_amount to tbl_sales_order_items';
    END

    -- Add total if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND name = 'total')
    BEGIN
        ALTER TABLE [dbo].[tbl_sales_order_items]
        ADD [total] DECIMAL(18,2) NOT NULL DEFAULT 0;
        PRINT '✓ Added total to tbl_sales_order_items';
    END
END
GO

-- ============================================
-- Fix tbl_stock_out - Add missing columns
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
BEGIN
    -- Add sales_order_id if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND name = 'sales_order_id')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_out]
        ADD [sales_order_id] INT NULL;
        
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
        BEGIN
            ALTER TABLE [dbo].[tbl_stock_out]
            ADD CONSTRAINT [FK_stock_out_sales_order] FOREIGN KEY ([sales_order_id]) REFERENCES [dbo].[tbl_sales_order]([sales_order_id]);
        END
        
        PRINT '✓ Added sales_order_id to tbl_stock_out';
    END

    -- Add stock_out_date if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND name = 'stock_out_date')
    BEGIN
        ALTER TABLE [dbo].[tbl_stock_out]
        ADD [stock_out_date] DATETIME NOT NULL DEFAULT GETDATE();
        PRINT '✓ Added stock_out_date to tbl_stock_out';
    END

    -- Add processed_by if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND name = 'processed_by')
    BEGIN
        -- Add as nullable first
        ALTER TABLE [dbo].[tbl_stock_out]
        ADD [processed_by] INT NULL;
        
        -- Set default value for existing rows
        DECLARE @defaultUserId3 INT;
        SELECT @defaultUserId3 = ISNULL(MIN(user_id), 1) FROM tbl_users;
        IF @defaultUserId3 IS NULL SET @defaultUserId3 = 1;
        
        UPDATE [dbo].[tbl_stock_out] SET [processed_by] = @defaultUserId3;
        
        -- Now make it NOT NULL
        ALTER TABLE [dbo].[tbl_stock_out]
        ALTER COLUMN [processed_by] INT NOT NULL;
        
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_users]') AND type in (N'U'))
        BEGIN
            ALTER TABLE [dbo].[tbl_stock_out]
            ADD CONSTRAINT [FK_stock_out_user] FOREIGN KEY ([processed_by]) REFERENCES [dbo].[tbl_users]([user_id]);
        END
        
        PRINT '✓ Added processed_by to tbl_stock_out';
    END
END
GO

-- ============================================
-- Verify tbl_purchase_order_items structure
-- ============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
BEGIN
    -- Check if po_items_id exists and is identity
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'po_items_id' AND is_identity = 1)
    BEGIN
        PRINT '✓ tbl_purchase_order_items structure is correct';
    END
    ELSE
    BEGIN
        PRINT '⚠ Warning: tbl_purchase_order_items may have structure issues';
    END
END
GO

PRINT '';
PRINT '=== All Tables Fixed! ===';
PRINT 'You can now sync your data again. The sync should work without errors.';
PRINT '';
PRINT 'Note: If tbl_purchase_order_items still has issues, you may need to:';
PRINT '1. Drop and recreate the table, or';
PRINT '2. Check the actual column name in your local database';
GO

