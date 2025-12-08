-- ============================================
-- Add Cancellation Reason and Remarks to Purchase Order
-- ============================================
-- This script adds cancellation_reason and cancellation_remarks columns
-- to tbl_purchase_order to allow users to record reasons when cancelling purchase orders
-- ============================================

PRINT '========================================';
PRINT 'Adding Cancellation Reason and Remarks Columns';
PRINT '========================================';
PRINT '';

-- ============================================
-- Add cancellation_reason to tbl_purchase_order
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'cancellation_reason')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    ADD [cancellation_reason] NVARCHAR(100) NULL;
    
    PRINT '✓ Added cancellation_reason column to tbl_purchase_order';
END
ELSE
BEGIN
    PRINT '⚠ cancellation_reason column already exists';
END
GO

-- ============================================
-- Add cancellation_remarks to tbl_purchase_order
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'cancellation_remarks')
BEGIN
    ALTER TABLE [dbo].[tbl_purchase_order]
    ADD [cancellation_remarks] NVARCHAR(1000) NULL;
    
    PRINT '✓ Added cancellation_remarks column to tbl_purchase_order';
END
ELSE
BEGIN
    PRINT '⚠ cancellation_remarks column already exists';
END
GO

PRINT '';
PRINT '========================================';
PRINT '✅ Cancellation Reason and Remarks Columns Added!';
PRINT '========================================';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Update PurchaseOrder model to include cancellation_reason and cancellation_remarks';
PRINT '  2. Update PurchaseOrderService to handle cancellation reason and remarks';
PRINT '  3. Update UI to show reason dropdown and remarks input when cancelling';
PRINT '';

