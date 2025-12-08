-- ============================================
-- Check and Fix Missing Supplier Names
-- ============================================
-- This script checks purchase orders with missing supplier names
-- and helps identify/fix the issue
-- ============================================

PRINT '========================================';
PRINT 'Checking Supplier Names for Purchase Orders';
PRINT '========================================';
PRINT '';

-- Check purchase orders with missing supplier names
PRINT 'Purchase Orders with Missing Supplier Names:';
PRINT '----------------------------------------';
SELECT 
    po.po_id,
    po.po_number,
    po.supplier_id,
    s.supplier_id AS supplier_exists,
    s.supplier_name,
    po.status,
    po.order_date
FROM tbl_purchase_order po
LEFT JOIN tbl_supplier s ON po.supplier_id = s.supplier_id
WHERE s.supplier_id IS NULL OR s.supplier_name IS NULL
ORDER BY po.po_id;
PRINT '';

-- Check all suppliers
PRINT 'All Suppliers in Database:';
PRINT '----------------------------------------';
SELECT 
    supplier_id,
    supplier_name,
    contact_number,
    email,
    is_active,
    created_date
FROM tbl_supplier
ORDER BY supplier_id;
PRINT '';

-- Check if supplier_id = 1 exists
PRINT 'Checking if Supplier ID 1 exists:';
PRINT '----------------------------------------';
IF EXISTS (SELECT 1 FROM tbl_supplier WHERE supplier_id = 1)
BEGIN
    SELECT 
        supplier_id,
        supplier_name,
        contact_number,
        email,
        is_active
    FROM tbl_supplier
    WHERE supplier_id = 1;
    PRINT '✓ Supplier ID 1 exists';
END
ELSE
BEGIN
    PRINT '⚠ Supplier ID 1 does NOT exist!';
    PRINT '';
    PRINT 'To fix this, you can either:';
    PRINT '  1. Create a supplier with ID 1 (if you need to keep the ID), OR';
    PRINT '  2. Update the purchase order to use an existing supplier ID';
    PRINT '';
    PRINT 'Example: Create a new supplier';
    PRINT '  INSERT INTO tbl_supplier (supplier_name, contact_number, email, is_active, created_date)';
    PRINT '  VALUES (''Default Supplier'', NULL, NULL, 1, GETDATE());';
    PRINT '';
    PRINT 'Then check the new supplier_id and update the purchase order:';
    PRINT '  UPDATE tbl_purchase_order';
    PRINT '  SET supplier_id = (SELECT MAX(supplier_id) FROM tbl_supplier)';
    PRINT '  WHERE supplier_id = 1;';
END
PRINT '';

PRINT '========================================';
PRINT '✅ Diagnostic Complete!';
PRINT '========================================';
PRINT '';




