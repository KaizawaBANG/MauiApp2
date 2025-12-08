-- Create Stock Out Tables
-- Automatically created when sales transactions are completed
-- Tracks items removed from inventory due to sales

-- Create Stock Out Header Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_out] (
        [stock_out_id] INT IDENTITY(1,1) PRIMARY KEY,
        [sales_order_id] INT NULL, -- Links to sale (nullable for future standalone stock out)
        [stock_out_number] NVARCHAR(50) NOT NULL UNIQUE, -- STO-001, STO-002, etc.
        [stock_out_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [reason] NVARCHAR(50) NOT NULL DEFAULT 'Sale', -- Sale, Damaged, Expired, etc.
        [processed_by] INT NOT NULL, -- User who processed
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_out_sales_order] FOREIGN KEY ([sales_order_id]) 
            REFERENCES [dbo].[tbl_sales_order]([sales_order_id]),
        CONSTRAINT [FK_stock_out_user] FOREIGN KEY ([processed_by]) 
            REFERENCES [dbo].[tbl_users]([user_id])
    );

    CREATE UNIQUE INDEX [IX_stock_out_number] ON [dbo].[tbl_stock_out] ([stock_out_number]);
    CREATE INDEX [IX_stock_out_sales_order] ON [dbo].[tbl_stock_out] ([sales_order_id]);
    CREATE INDEX [IX_stock_out_date] ON [dbo].[tbl_stock_out] ([stock_out_date]);

    PRINT 'Stock Out table created successfully!';
END
ELSE
BEGIN
    PRINT 'Stock Out table already exists.';
END
GO

-- Create Stock Out Items Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_out_items] (
        [stock_out_items_id] INT IDENTITY(1,1) PRIMARY KEY,
        [stock_out_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity] INT NOT NULL,
        [reason] NVARCHAR(50) NOT NULL DEFAULT 'Sale',
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_out_items_stock_out] FOREIGN KEY ([stock_out_id]) 
            REFERENCES [dbo].[tbl_stock_out]([stock_out_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_stock_out_items_product] FOREIGN KEY ([product_id]) 
            REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_out] CHECK ([quantity] > 0)
    );

    CREATE INDEX [IX_stock_out_items_stock_out] ON [dbo].[tbl_stock_out_items] ([stock_out_id]);
    CREATE INDEX [IX_stock_out_items_product] ON [dbo].[tbl_stock_out_items] ([product_id]);

    PRINT 'Stock Out Items table created successfully!';
END
ELSE
BEGIN
    PRINT 'Stock Out Items table already exists.';
END
GO







