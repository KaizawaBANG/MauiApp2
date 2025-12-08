-- ============================================
-- Add Tax Support to Purchase Order
-- ============================================
-- This script adds tax_id, subtotal, and tax_amount columns
-- to tbl_purchase_order for proper tax calculation
-- ============================================

PRINT '========================================';
PRINT 'Adding Tax Support to Purchase Order';
PRINT '========================================';
PRINT '';

-- ============================================
-- Add tax_id to tbl_purchase_order
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'tax_id')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    ADD [tax_id] INT NULL;
    
    PRINT '✓ Added tax_id column to tbl_purchase_order';
    
    -- Add foreign key constraint if tbl_tax exists
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_tax]') AND type in (N'U'))
    BEGIN
        ALTER TABLE [dbo].[tbl_purchase_order]
        ADD CONSTRAINT [FK_purchase_order_tax] 
        FOREIGN KEY ([tax_id]) 
        REFERENCES [dbo].[tbl_tax] ([tax_id]);
        
        PRINT '✓ Added foreign key constraint to tbl_tax';
    END
END
ELSE
BEGIN
    PRINT '⚠ tax_id column already exists';
END
GO

-- ============================================
-- Add subtotal to tbl_purchase_order
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'subtotal')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    ADD [subtotal] DECIMAL(12,2) NULL;
    
    PRINT '✓ Added subtotal column to tbl_purchase_order';
    
    -- Update existing records: set subtotal = total_amount (assuming no tax was applied before)
    UPDATE [dbo].[tbl_purchase_order]
    SET [subtotal] = [total_amount]
    WHERE [subtotal] IS NULL;
    
    PRINT '✓ Populated subtotal for existing records';
END
ELSE
BEGIN
    PRINT '⚠ subtotal column already exists';
END
GO

-- ============================================
-- Add tax_amount to tbl_purchase_order
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'tax_amount')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    ADD [tax_amount] DECIMAL(12,2) NULL DEFAULT 0;
    
    PRINT '✓ Added tax_amount column to tbl_purchase_order';
    
    -- Set tax_amount to 0 for existing records
    UPDATE [dbo].[tbl_purchase_order]
    SET [tax_amount] = 0
    WHERE [tax_amount] IS NULL;
    
    PRINT '✓ Set tax_amount to 0 for existing records';
END
ELSE
BEGIN
    PRINT '⚠ tax_amount column already exists';
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Tax Support Added to Purchase Order!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Update PurchaseOrder model to include tax_id, subtotal, tax_amount';
PRINT '  2. Update PurchaseOrderService to calculate tax';
PRINT '  3. Update UI to show tax selection and breakdown';
PRINT '';




