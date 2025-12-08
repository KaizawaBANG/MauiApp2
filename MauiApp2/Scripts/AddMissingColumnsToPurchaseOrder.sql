-- ============================================
-- Add Missing Columns to Purchase Order
-- ============================================
-- This script adds subtotal and tax_amount columns
-- to tbl_purchase_order if they don't exist
-- ============================================

PRINT '========================================';
PRINT 'Adding Missing Columns to Purchase Order';
PRINT '========================================';
PRINT '';

-- ============================================
-- Add subtotal to tbl_purchase_order
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'subtotal')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    ADD [subtotal] DECIMAL(12,2) NULL;
    
    PRINT '✓ Added subtotal column to tbl_purchase_order';
END
ELSE
BEGIN
    PRINT '⚠ subtotal column already exists';
END
GO

-- Calculate subtotal from items for existing POs (separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'subtotal')
BEGIN
    UPDATE po
    SET po.subtotal = ISNULL((
        SELECT SUM(poi.quantity_ordered * poi.unit_cost)
        FROM tbl_purchase_order_items poi
        WHERE poi.po_id = po.po_id
    ), po.total_amount)
    FROM tbl_purchase_order po
    WHERE po.subtotal IS NULL;
    
    PRINT '✓ Calculated subtotal for existing purchase orders';
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
END
ELSE
BEGIN
    PRINT '⚠ tax_amount column already exists';
END
GO

-- Calculate tax_amount from items for existing POs (separate batch)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'tax_amount')
BEGIN
    -- First, check if items have tax_amount column
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'tax_amount')
    BEGIN
        UPDATE po
        SET po.tax_amount = ISNULL((
            SELECT SUM(poi.tax_amount)
            FROM tbl_purchase_order_items poi
            WHERE poi.po_id = po.po_id
        ), 0)
        FROM tbl_purchase_order po
        WHERE po.tax_amount IS NULL;
        
        PRINT '✓ Calculated tax_amount from items for existing purchase orders';
    END
    ELSE
    BEGIN
        -- If items don't have tax_amount yet, set to 0
        UPDATE [dbo].[tbl_purchase_order]
        SET [tax_amount] = 0
        WHERE [tax_amount] IS NULL;
        
        PRINT '✓ Set tax_amount to 0 for existing purchase orders (items table needs migration)';
    END
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Missing Columns Added!';
PRINT '========================================';
PRINT '';
PRINT 'Your tbl_purchase_order now has:';
PRINT '  ✓ subtotal - Sum of all item subtotals';
PRINT '  ✓ tax_amount - Sum of all item tax amounts';
PRINT '  ✓ total_amount - Grand total (subtotal + tax_amount)';
PRINT '';
PRINT 'Note: If you still have tax_id column, you can remove it using RemoveTaxIdFromPurchaseOrder.sql';
PRINT '';

