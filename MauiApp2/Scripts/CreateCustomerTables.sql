-- Create Customer Table
-- For managing customer information

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_customer]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_customer] (
        [customer_id] INT IDENTITY(1,1) PRIMARY KEY,
        [customer_name] NVARCHAR(255) NOT NULL,
        [contact_number] NVARCHAR(50) NULL,
        [email] NVARCHAR(255) NULL,
        [address] NVARCHAR(MAX) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [UQ_customer_email] UNIQUE ([email])
    );

    CREATE INDEX [IX_customer_name] ON [dbo].[tbl_customer] ([customer_name]);
    CREATE INDEX [IX_customer_email] ON [dbo].[tbl_customer] ([email]);
    CREATE INDEX [IX_customer_active] ON [dbo].[tbl_customer] ([is_active]);

    PRINT 'Customer table created successfully!';
END
ELSE
BEGIN
    PRINT 'Customer table already exists.';
END
GO

