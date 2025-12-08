-- ============================================
-- Remove tax_id from Purchase Order
-- ============================================
-- This script removes the tax_id column and foreign key constraint
-- from tbl_purchase_order since tax is now calculated per item
-- ============================================

PRINT '========================================';
PRINT 'Removing tax_id from Purchase Order';
PRINT '========================================';
PRINT '';

-- ============================================
-- Remove foreign key constraint if it exists
-- ============================================
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_purchase_order_tax')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    DROP CONSTRAINT [FK_purchase_order_tax];
    
    PRINT '✓ Removed FK_purchase_order_tax foreign key constraint';
END
ELSE
BEGIN
    PRINT '⚠ FK_purchase_order_tax constraint does not exist';
END
GO

-- ============================================
-- Remove tax_id column if it exists
-- ============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'tax_id')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    DROP COLUMN [tax_id];
    
    PRINT '✓ Removed tax_id column from tbl_purchase_order';
END
ELSE
BEGIN
    PRINT '⚠ tax_id column does not exist in tbl_purchase_order';
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ tax_id Removed from Purchase Order!';
PRINT '========================================';
PRINT '';
PRINT 'Note: Tax is now calculated per item in tbl_purchase_order_items';
PRINT '      The PO header still has subtotal and tax_amount (sum of all items)';
PRINT '';




