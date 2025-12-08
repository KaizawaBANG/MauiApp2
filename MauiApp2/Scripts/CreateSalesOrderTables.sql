-- Create Sales Order Tables
-- For sales transactions (Point of Sale system)
-- Automatically creates Stock Out when sale is completed

-- Create Sales Order Header Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_sales_order] (
        [sales_order_id] INT IDENTITY(1,1) PRIMARY KEY,
        [sales_order_number] NVARCHAR(50) NOT NULL UNIQUE, -- INV-001, INV-002, etc.
        [sales_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [subtotal] DECIMAL(18,2) NOT NULL DEFAULT 0, -- Before tax
        [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0, -- Total tax
        [total_amount] DECIMAL(18,2) NOT NULL DEFAULT 0, -- Final total
        [payment_method] NVARCHAR(50) NOT NULL, -- Cash, Card, etc.
        [processed_by] INT NOT NULL, -- User who processed the sale
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_sales_order_user] FOREIGN KEY ([processed_by]) 
            REFERENCES [dbo].[tbl_users]([user_id])
    );

    CREATE UNIQUE INDEX [IX_sales_order_number] ON [dbo].[tbl_sales_order] ([sales_order_number]);
    CREATE INDEX [IX_sales_order_date] ON [dbo].[tbl_sales_order] ([sales_date]);
    CREATE INDEX [IX_sales_order_user] ON [dbo].[tbl_sales_order] ([processed_by]);

    PRINT 'Sales Order table created successfully!';
END
ELSE
BEGIN
    PRINT 'Sales Order table already exists.';
END
GO

-- Create Sales Order Items Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_sales_order_items] (
        [sales_order_item_id] INT IDENTITY(1,1) PRIMARY KEY,
        [sales_order_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity] INT NOT NULL,
        [unit_price] DECIMAL(18,2) NOT NULL, -- Price at time of sale
        [tax_rate] DECIMAL(5,2) NOT NULL DEFAULT 0, -- Tax % (from product.tax_id)
        [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0, -- Tax for this item
        [subtotal] DECIMAL(18,2) NOT NULL, -- quantity × unit_price
        [total] DECIMAL(18,2) NOT NULL, -- subtotal + tax_amount
        CONSTRAINT [FK_sales_order_items_sales_order] FOREIGN KEY ([sales_order_id]) 
            REFERENCES [dbo].[tbl_sales_order]([sales_order_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_sales_order_items_product] FOREIGN KEY ([product_id]) 
            REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_sold] CHECK ([quantity] > 0),
        CONSTRAINT [CK_unit_price_sold] CHECK ([unit_price] >= 0)
    );

    CREATE INDEX [IX_sales_order_items_sales_order] ON [dbo].[tbl_sales_order_items] ([sales_order_id]);
    CREATE INDEX [IX_sales_order_items_product] ON [dbo].[tbl_sales_order_items] ([product_id]);

    PRINT 'Sales Order Items table created successfully!';
END
ELSE
BEGIN
    PRINT 'Sales Order Items table already exists.';
END
GO







