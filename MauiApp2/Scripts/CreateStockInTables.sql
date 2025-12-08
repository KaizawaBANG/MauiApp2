-- Create Stock In Tables
-- Simple structure for receiving inventory from Purchase Orders

-- Create Stock In Header Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_in] (
        [stock_in_id] INT IDENTITY(1,1) PRIMARY KEY,
        [po_id] INT NOT NULL, -- Links to purchase order
        [supplier_id] INT NULL, -- Supplier from PO
        [stock_in_number] NVARCHAR(50) NOT NULL, -- Auto-generated: SI-001, SI-002, etc.
        [stock_in_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [notes] NVARCHAR(MAX) NULL,
        [processed_by] INT NOT NULL, -- User who processed
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_in_po] FOREIGN KEY ([po_id]) 
            REFERENCES [dbo].[tbl_purchase_order]([po_id]),
        CONSTRAINT [FK_stock_in_supplier] FOREIGN KEY ([supplier_id]) 
            REFERENCES [dbo].[tbl_supplier]([supplier_id]),
        CONSTRAINT [FK_stock_in_user] FOREIGN KEY ([processed_by]) 
            REFERENCES [dbo].[tbl_users]([user_id])
    );

    CREATE UNIQUE INDEX [IX_stock_in_number] ON [dbo].[tbl_stock_in] ([stock_in_number]);
    CREATE INDEX [IX_stock_in_po] ON [dbo].[tbl_stock_in] ([po_id]);
    CREATE INDEX [IX_stock_in_date] ON [dbo].[tbl_stock_in] ([stock_in_date]);

    PRINT 'Stock In table created successfully!';
END
ELSE
BEGIN
    PRINT 'Stock In table already exists.';
END
GO

-- Create Stock In Items Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_in_items] (
        [stock_in_items_id] INT IDENTITY(1,1) PRIMARY KEY,
        [stock_in_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity_received] INT NOT NULL,
        [unit_cost] DECIMAL(18,2) NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_in_items_stock_in] FOREIGN KEY ([stock_in_id]) 
            REFERENCES [dbo].[tbl_stock_in]([stock_in_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_stock_in_items_product] FOREIGN KEY ([product_id]) 
            REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_received] CHECK ([quantity_received] > 0),
        CONSTRAINT [CK_unit_cost_received] CHECK ([unit_cost] >= 0)
    );

    CREATE INDEX [IX_stock_in_items_stock_in] ON [dbo].[tbl_stock_in_items] ([stock_in_id]);
    CREATE INDEX [IX_stock_in_items_product] ON [dbo].[tbl_stock_in_items] ([product_id]);

    PRINT 'Stock In Items table created successfully!';
END
ELSE
BEGIN
    PRINT 'Stock In Items table already exists.';
END
GO







