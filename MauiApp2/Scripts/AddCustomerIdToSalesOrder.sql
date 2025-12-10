-- Add customer_id column to tbl_sales_order table
-- This allows sales orders to be associated with customers

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND name = 'customer_id')
BEGIN
    ALTER TABLE [dbo].[tbl_sales_order]
    ADD [customer_id] INT NULL; -- NULL for walk-in customers
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[tbl_sales_order]
    ADD CONSTRAINT [FK_sales_order_customer] FOREIGN KEY ([customer_id]) 
        REFERENCES [dbo].[tbl_customer]([customer_id]);
    
    -- Add index for better query performance
    CREATE INDEX [IX_sales_order_customer] ON [dbo].[tbl_sales_order] ([customer_id]);
    
    PRINT 'customer_id column added to tbl_sales_order successfully!';
END
ELSE
BEGIN
    PRINT 'customer_id column already exists in tbl_sales_order.';
END
GO

