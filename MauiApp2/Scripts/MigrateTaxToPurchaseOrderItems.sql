-- ============================================
-- Migrate Tax from Purchase Order to Items
-- ============================================
-- This script migrates tax calculation from PO level to item level
-- Each item will have its own tax based on product's tax_id
-- ============================================

PRINT '========================================';
PRINT 'Migrating Tax to Purchase Order Items';
PRINT '========================================';
PRINT '';

-- ============================================
-- Step 1: Add tax fields to purchase_order_items
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'tax_rate')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order_items]
    ADD [tax_rate] DECIMAL(5,2) NOT NULL DEFAULT 0;
    PRINT '✓ Added tax_rate column to tbl_purchase_order_items';
END
ELSE
BEGIN
    PRINT '⚠ tax_rate column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'tax_amount')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order_items]
    ADD [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT '✓ Added tax_amount column to tbl_purchase_order_items';
END
ELSE
BEGIN
    PRINT '⚠ tax_amount column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'subtotal')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order_items]
    ADD [subtotal] DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT '✓ Added subtotal column to tbl_purchase_order_items';
END
ELSE
BEGIN
    PRINT '⚠ subtotal column already exists';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND name = 'total')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order_items]
    ADD [total] DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT '✓ Added total column to tbl_purchase_order_items';
END
ELSE
BEGIN
    PRINT '⚠ total column already exists';
END
GO

-- ============================================
-- Step 2: Calculate and populate tax for existing items
-- ============================================
PRINT '';
PRINT 'Calculating tax for existing purchase order items...';

UPDATE poi
SET 
    poi.subtotal = poi.quantity_ordered * poi.unit_cost,
    poi.tax_rate = ISNULL(t.tax_rate, 0),
    poi.tax_amount = (poi.quantity_ordered * poi.unit_cost) * ISNULL(t.tax_rate, 0),
    poi.total = (poi.quantity_ordered * poi.unit_cost) * (1 + ISNULL(t.tax_rate, 0))
FROM tbl_purchase_order_items poi
LEFT JOIN tbl_product p ON poi.product_id = p.product_id
LEFT JOIN tbl_tax t ON p.tax_id = t.tax_id
WHERE poi.subtotal = 0 OR poi.total = 0;

PRINT '✓ Updated existing purchase order items with tax calculations';
GO

-- ============================================
-- Step 3: Remove tax_id from purchase_order (optional - keep for backward compatibility)
-- ============================================
-- Note: We'll keep tax_id column for now to maintain backward compatibility
-- It can be removed later if not needed
PRINT '';
PRINT '⚠ Keeping tax_id in tbl_purchase_order for backward compatibility';
PRINT '   You can remove it later if not needed';
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Tax Migration Complete!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Update PurchaseOrderService to calculate tax per item';
PRINT '  2. Update UI to show tax per item';
PRINT '  3. Remove tax dropdown from PO creation form';
PRINT '';




