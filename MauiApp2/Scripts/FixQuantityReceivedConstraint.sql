-- Fix CK_quantity_received constraint to allow 0 (for cases where all items are rejected)
-- This allows recording stock in items where quantity_received = 0 and quantity_rejected > 0

PRINT '=== Fixing CK_quantity_received Constraint ===';
PRINT '';

-- Drop the existing constraint if it exists
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_quantity_received' AND parent_object_id = OBJECT_ID('tbl_stock_in_items'))
BEGIN
    ALTER TABLE [dbo].[tbl_stock_in_items] DROP CONSTRAINT [CK_quantity_received];
    PRINT '✓ Dropped existing CK_quantity_received constraint';
END
ELSE
BEGIN
    PRINT '⚠ CK_quantity_received constraint does not exist';
END
GO

-- Add the new constraint that allows 0 (for rejected items)
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_quantity_received' AND parent_object_id = OBJECT_ID('tbl_stock_in_items'))
BEGIN
    ALTER TABLE [dbo].[tbl_stock_in_items]
    ADD CONSTRAINT [CK_quantity_received] CHECK ([quantity_received] >= 0);
    PRINT '✓ Added new CK_quantity_received constraint (allows >= 0)';
END
GO

PRINT '';
PRINT '=== Constraint Fix Complete ===';
PRINT 'Note: quantity_received can now be 0 to allow recording rejected items.';
PRINT 'However, at least one of quantity_received or quantity_rejected should be > 0.';




