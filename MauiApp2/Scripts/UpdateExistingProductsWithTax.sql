-- Update existing products with NULL tax_id to assign default VAT 12% tax
-- This script assigns the default VAT tax (tax_id = 1) to all products that currently have NULL tax_id

-- First, check if VAT tax exists (assuming it's tax_id = 1 based on common setup)
IF EXISTS (SELECT 1 FROM [dbo].[tbl_tax] WHERE tax_id = 1 AND tax_name LIKE '%VAT%')
BEGIN
    -- Update all products with NULL tax_id to VAT 12%
    UPDATE [dbo].[tbl_product]
    SET [tax_id] = 1
    WHERE [tax_id] IS NULL;
    
    PRINT 'Updated existing products with NULL tax_id to VAT 12% (tax_id = 1)';
    PRINT 'Number of products updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
END
ELSE IF EXISTS (SELECT 1 FROM [dbo].[tbl_tax] WHERE tax_type = 'VAT' AND tax_rate = 0.12)
BEGIN
    -- Find VAT 12% tax and assign it to products with NULL tax_id
    DECLARE @vat_tax_id INT;
    SELECT TOP 1 @vat_tax_id = tax_id FROM [dbo].[tbl_tax] WHERE tax_type = 'VAT' AND tax_rate = 0.12;
    
    UPDATE [dbo].[tbl_product]
    SET [tax_id] = @vat_tax_id
    WHERE [tax_id] IS NULL;
    
    PRINT 'Updated existing products with NULL tax_id to VAT 12% (tax_id = ' + CAST(@vat_tax_id AS NVARCHAR(10)) + ')';
    PRINT 'Number of products updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
END
ELSE
BEGIN
    -- Get the first active tax if no VAT is found
    DECLARE @default_tax_id INT;
    SELECT TOP 1 @default_tax_id = tax_id FROM [dbo].[tbl_tax] WHERE is_active = 1 ORDER BY tax_id;
    
    IF @default_tax_id IS NOT NULL
    BEGIN
        UPDATE [dbo].[tbl_product]
        SET [tax_id] = @default_tax_id
        WHERE [tax_id] IS NULL;
        
        PRINT 'Updated existing products with NULL tax_id to default active tax (tax_id = ' + CAST(@default_tax_id AS NVARCHAR(10)) + ')';
        PRINT 'Number of products updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
    END
    ELSE
    BEGIN
        PRINT 'ERROR: No active tax found in tbl_tax table. Please create taxes first.';
    END
END
GO


