-- Add tax_id column to tbl_product table to connect products with taxes
-- This script adds a foreign key relationship between products and taxes

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    -- Check if tax_id column already exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND name = 'tax_id')
    BEGIN
        -- Add tax_id column as nullable foreign key
        ALTER TABLE [dbo].[tbl_product]
        ADD [tax_id] INT NULL;

        -- Create foreign key constraint to tbl_tax
        IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_tax]') AND type in (N'U'))
        BEGIN
            ALTER TABLE [dbo].[tbl_product]
            ADD CONSTRAINT [FK_tbl_product_tbl_tax] 
            FOREIGN KEY ([tax_id]) 
            REFERENCES [dbo].[tbl_tax] ([tax_id]);

            PRINT 'tax_id column added to tbl_product table with foreign key constraint to tbl_tax.';
        END
        ELSE
        BEGIN
            PRINT 'tax_id column added to tbl_product table. Foreign key constraint not created because tbl_tax table does not exist yet.';
        END

        -- Create index on tax_id for faster lookups
        CREATE INDEX [IX_tbl_product_tax_id] ON [dbo].[tbl_product] ([tax_id]);

        PRINT 'Index created on tax_id column.';
    END
    ELSE
    BEGIN
        PRINT 'tax_id column already exists in tbl_product table.';
    END
END
ELSE
BEGIN
    PRINT 'tbl_product table does not exist. Please create the table first.';
END
GO


