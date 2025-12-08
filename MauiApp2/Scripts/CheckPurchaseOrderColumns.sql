-- ============================================
-- Check Purchase Order Table Structure
-- ============================================
-- This script checks what columns exist in tbl_purchase_order
-- ============================================

PRINT '========================================';
PRINT 'Checking tbl_purchase_order Columns';
PRINT '========================================';
PRINT '';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tbl_purchase_order'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '========================================';
PRINT 'Checking Foreign Keys';
PRINT '========================================';
PRINT '';

SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fc ON fk.object_id = fc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) = 'tbl_purchase_order';

PRINT '';
PRINT '========================================';
PRINT 'Summary';
PRINT '========================================';
PRINT '';

DECLARE @HasSubtotal BIT = 0;
DECLARE @HasTaxAmount BIT = 0;
DECLARE @HasTaxId BIT = 0;
DECLARE @HasCancellationReason BIT = 0;
DECLARE @HasCancellationRemarks BIT = 0;

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'subtotal')
    SET @HasSubtotal = 1;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'tax_amount')
    SET @HasTaxAmount = 1;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'tax_id')
    SET @HasTaxId = 1;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'cancellation_reason')
    SET @HasCancellationReason = 1;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND name = 'cancellation_remarks')
    SET @HasCancellationRemarks = 1;

PRINT 'Column Status:';
PRINT '  subtotal: ' + CASE WHEN @HasSubtotal = 1 THEN '✓ EXISTS' ELSE '✗ MISSING' END;
PRINT '  tax_amount: ' + CASE WHEN @HasTaxAmount = 1 THEN '✓ EXISTS' ELSE '✗ MISSING' END;
PRINT '  tax_id: ' + CASE WHEN @HasTaxId = 1 THEN '⚠ EXISTS (should be removed)' ELSE '✓ Does not exist' END;
PRINT '  cancellation_reason: ' + CASE WHEN @HasCancellationReason = 1 THEN '✓ EXISTS' ELSE '✗ MISSING' END;
PRINT '  cancellation_remarks: ' + CASE WHEN @HasCancellationRemarks = 1 THEN '✓ EXISTS' ELSE '✗ MISSING' END;
PRINT '';




