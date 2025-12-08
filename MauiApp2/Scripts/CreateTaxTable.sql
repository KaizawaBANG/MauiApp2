-- Create Tax Table for Philippines Tax System
-- This script creates the tbl_tax table for managing tax rates

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_tax]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_tax] (
        [tax_id] INT IDENTITY(1,1) PRIMARY KEY,
        [tax_name] NVARCHAR(100) NOT NULL,
        [tax_type] NVARCHAR(50) NOT NULL,
        [tax_rate] DECIMAL(5,4) NOT NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [CK_tax_rate] CHECK ([tax_rate] >= 0 AND [tax_rate] <= 1)
    );

    -- Create index on tax_name for faster lookups
    CREATE INDEX [IX_tax_name] ON [dbo].[tbl_tax] ([tax_name]);

    -- Insert Philippines tax data
    -- VAT (Value Added Tax) is 12% in the Philippines
    INSERT INTO [dbo].[tbl_tax] ([tax_name], [tax_type], [tax_rate], [is_active])
    VALUES 
        ('VAT', 'VAT', 0.12, 1),
        ('Zero Rated VAT', 'VAT', 0.0000, 1),
        ('Exempt', 'Exempt', 0.0000, 1);

    PRINT 'Tax table created successfully with Philippines tax data!';
END
ELSE
BEGIN
    PRINT 'Tax table already exists.';
    -- Add default Philippines VAT if table exists but is empty
    IF NOT EXISTS (SELECT 1 FROM [dbo].[tbl_tax] WHERE tax_name = 'VAT')
    BEGIN
        INSERT INTO [dbo].[tbl_tax] ([tax_name], [tax_type], [tax_rate], [is_active])
        VALUES ('VAT', 'VAT', 0.12, 1);
        PRINT 'Philippines VAT (12%) added to existing tax table.';
    END
END
GO

