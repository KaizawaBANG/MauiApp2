-- Create Purchase Order Tables
-- This script creates tbl_purchase_order and tbl_purchase_order_items tables

-- Create Purchase Order Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_purchase_order] (
        [po_id] INT IDENTITY(1,1) PRIMARY KEY,
        [supplier_id] INT NOT NULL,
        [po_number] NVARCHAR(50) NOT NULL,
        [order_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [expected_date] DATETIME NOT NULL,
        [status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [total_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [notes] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [FK_purchase_order_supplier] FOREIGN KEY ([supplier_id]) 
            REFERENCES [dbo].[tbl_supplier]([supplier_id])
    );

    -- Create unique constraint on po_number
    CREATE UNIQUE INDEX [IX_po_number] ON [dbo].[tbl_purchase_order] ([po_number]);

    -- Create index on supplier_id for faster lookups
    CREATE INDEX [IX_purchase_order_supplier] ON [dbo].[tbl_purchase_order] ([supplier_id]);

    -- Create index on status for filtering
    CREATE INDEX [IX_purchase_order_status] ON [dbo].[tbl_purchase_order] ([status]);

    -- Create index on order_date for sorting
    CREATE INDEX [IX_purchase_order_date] ON [dbo].[tbl_purchase_order] ([order_date]);

    PRINT 'Purchase Order table created successfully!';
END
ELSE
BEGIN
    PRINT 'Purchase Order table already exists.';
END
GO

-- Create Purchase Order Items Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_purchase_order_items] (
        [po_items_id] INT IDENTITY(1,1) PRIMARY KEY,
        [po_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity_ordered] INT NOT NULL,
        [unit_cost] DECIMAL(18,2) NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_purchase_order_items_po] FOREIGN KEY ([po_id]) 
            REFERENCES [dbo].[tbl_purchase_order]([po_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_purchase_order_items_product] FOREIGN KEY ([product_id]) 
            REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_ordered] CHECK ([quantity_ordered] > 0),
        CONSTRAINT [CK_unit_cost] CHECK ([unit_cost] >= 0)
    );

    -- Create index on po_id for faster lookups
    CREATE INDEX [IX_purchase_order_items_po] ON [dbo].[tbl_purchase_order_items] ([po_id]);

    -- Create index on product_id for faster lookups
    CREATE INDEX [IX_purchase_order_items_product] ON [dbo].[tbl_purchase_order_items] ([product_id]);

    PRINT 'Purchase Order Items table created successfully!';
END
ELSE
BEGIN
    PRINT 'Purchase Order Items table already exists.';
END
GO


