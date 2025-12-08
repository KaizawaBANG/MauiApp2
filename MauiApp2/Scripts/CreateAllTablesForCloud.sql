-- Create All Tables for Cloud Database
-- Run this script on your MonsterAPI cloud database to create all required tables
-- Tables are created in the correct order to respect foreign key dependencies

PRINT '=== Creating All Tables for Cloud Database ===';
PRINT '';

-- ============================================
-- 1. Create tbl_roles
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_roles] (
        [role_id] INT IDENTITY(1,1) PRIMARY KEY,
        [role_name] NVARCHAR(100) NOT NULL,
        [description] NVARCHAR(500) NULL
    );
    
    CREATE INDEX [IX_role_name] ON [dbo].[tbl_roles] ([role_name]);
    PRINT '✓ Created tbl_roles';
END
ELSE
BEGIN
    PRINT '⚠ tbl_roles already exists';
END
GO

-- ============================================
-- 2. Create tbl_users
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_users] (
        [user_id] INT IDENTITY(1,1) PRIMARY KEY,
        [role_id] INT NOT NULL,
        [username] NVARCHAR(50) NOT NULL,
        [email] NVARCHAR(100) NOT NULL,
        [password_hash] NVARCHAR(255) NOT NULL,
        [full_name] NVARCHAR(100) NOT NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [last_login] DATETIME NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_users_roles] FOREIGN KEY ([role_id]) REFERENCES [dbo].[tbl_roles]([role_id])
    );
    
    CREATE UNIQUE INDEX [IX_username] ON [dbo].[tbl_users] ([username]);
    CREATE UNIQUE INDEX [IX_email] ON [dbo].[tbl_users] ([email]);
    CREATE INDEX [IX_role_id] ON [dbo].[tbl_users] ([role_id]);
    PRINT '✓ Created tbl_users';
END
ELSE
BEGIN
    PRINT '⚠ tbl_users already exists';
END
GO

-- ============================================
-- 3. Create tbl_category
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_category]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_category] (
        [category_id] INT IDENTITY(1,1) PRIMARY KEY,
        [category_name] NVARCHAR(100) NOT NULL,
        [description] NVARCHAR(500) NULL
    );
    
    CREATE INDEX [IX_category_name] ON [dbo].[tbl_category] ([category_name]);
    PRINT '✓ Created tbl_category';
END
ELSE
BEGIN
    PRINT '⚠ tbl_category already exists';
END
GO

-- ============================================
-- 4. Create tbl_brand
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_brand]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_brand] (
        [brand_id] INT IDENTITY(1,1) PRIMARY KEY,
        [brand_name] NVARCHAR(100) NOT NULL,
        [description] NVARCHAR(500) NULL
    );
    
    CREATE INDEX [IX_brand_name] ON [dbo].[tbl_brand] ([brand_name]);
    PRINT '✓ Created tbl_brand';
END
ELSE
BEGIN
    PRINT '⚠ tbl_brand already exists';
END
GO

-- ============================================
-- 5. Create tbl_tax
-- ============================================
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
    
    CREATE INDEX [IX_tax_name] ON [dbo].[tbl_tax] ([tax_name]);
    PRINT '✓ Created tbl_tax';
END
ELSE
BEGIN
    PRINT '⚠ tbl_tax already exists';
END
GO

-- ============================================
-- 6. Create tbl_product
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_product]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_product] (
        [product_id] INT IDENTITY(1,1) PRIMARY KEY,
        [brand_id] INT NULL,
        [category_id] INT NULL,
        [tax_id] INT NULL,
        [product_name] NVARCHAR(200) NOT NULL,
        [product_sku] NVARCHAR(100) NOT NULL,
        [model_number] NVARCHAR(100) NULL,
        [cost_price] DECIMAL(18,2) NULL,
        [sell_price] DECIMAL(18,2) NOT NULL,
        [quantity] INT NULL DEFAULT 0,
        [status] BIT NULL DEFAULT 1,
        [created_date] DATETIME NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [FK_product_brand] FOREIGN KEY ([brand_id]) REFERENCES [dbo].[tbl_brand]([brand_id]),
        CONSTRAINT [FK_product_category] FOREIGN KEY ([category_id]) REFERENCES [dbo].[tbl_category]([category_id]),
        CONSTRAINT [FK_product_tax] FOREIGN KEY ([tax_id]) REFERENCES [dbo].[tbl_tax]([tax_id])
    );
    
    CREATE UNIQUE INDEX [IX_product_sku] ON [dbo].[tbl_product] ([product_sku]);
    CREATE INDEX [IX_product_name] ON [dbo].[tbl_product] ([product_name]);
    CREATE INDEX [IX_brand_id] ON [dbo].[tbl_product] ([brand_id]);
    CREATE INDEX [IX_category_id] ON [dbo].[tbl_product] ([category_id]);
    CREATE INDEX [IX_tax_id] ON [dbo].[tbl_product] ([tax_id]);
    PRINT '✓ Created tbl_product';
END
ELSE
BEGIN
    PRINT '⚠ tbl_product already exists';
END
GO

-- ============================================
-- 7. Create tbl_supplier
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_supplier]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_supplier] (
        [supplier_id] INT IDENTITY(1,1) PRIMARY KEY,
        [supplier_name] NVARCHAR(100) NOT NULL,
        [contact_number] NVARCHAR(20) NULL,
        [email] NVARCHAR(100) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL
    );
    
    CREATE INDEX [IX_supplier_name] ON [dbo].[tbl_supplier] ([supplier_name]);
    CREATE UNIQUE INDEX [IX_supplier_email] ON [dbo].[tbl_supplier] ([email]) WHERE [email] IS NOT NULL;
    PRINT '✓ Created tbl_supplier';
END
ELSE
BEGIN
    PRINT '⚠ tbl_supplier already exists';
END
GO

-- ============================================
-- 8. Create tbl_purchase_order
-- ============================================
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
        CONSTRAINT [FK_purchase_order_supplier] FOREIGN KEY ([supplier_id]) REFERENCES [dbo].[tbl_supplier]([supplier_id])
    );
    
    CREATE UNIQUE INDEX [IX_po_number] ON [dbo].[tbl_purchase_order] ([po_number]);
    CREATE INDEX [IX_purchase_order_supplier] ON [dbo].[tbl_purchase_order] ([supplier_id]);
    CREATE INDEX [IX_purchase_order_status] ON [dbo].[tbl_purchase_order] ([status]);
    CREATE INDEX [IX_purchase_order_date] ON [dbo].[tbl_purchase_order] ([order_date]);
    PRINT '✓ Created tbl_purchase_order';
END
ELSE
BEGIN
    PRINT '⚠ tbl_purchase_order already exists';
END
GO

-- ============================================
-- 9. Create tbl_purchase_order_items
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_purchase_order_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_purchase_order_items] (
        [po_items_id] INT IDENTITY(1,1) PRIMARY KEY,
        [po_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity_ordered] INT NOT NULL,
        [unit_cost] DECIMAL(18,2) NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_purchase_order_items_po] FOREIGN KEY ([po_id]) REFERENCES [dbo].[tbl_purchase_order]([po_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_purchase_order_items_product] FOREIGN KEY ([product_id]) REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_ordered] CHECK ([quantity_ordered] > 0),
        CONSTRAINT [CK_unit_cost] CHECK ([unit_cost] >= 0)
    );
    
    CREATE INDEX [IX_purchase_order_items_po] ON [dbo].[tbl_purchase_order_items] ([po_id]);
    CREATE INDEX [IX_purchase_order_items_product] ON [dbo].[tbl_purchase_order_items] ([product_id]);
    PRINT '✓ Created tbl_purchase_order_items';
END
ELSE
BEGIN
    PRINT '⚠ tbl_purchase_order_items already exists';
END
GO

-- ============================================
-- 10. Create tbl_stock_in
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_in] (
        [stock_in_id] INT IDENTITY(1,1) PRIMARY KEY,
        [po_id] INT NULL,
        [supplier_id] INT NULL,
        [stock_in_number] NVARCHAR(50) NOT NULL,
        [received_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [status] NVARCHAR(50) NOT NULL DEFAULT 'Received',
        [notes] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [FK_stock_in_po] FOREIGN KEY ([po_id]) REFERENCES [dbo].[tbl_purchase_order]([po_id]),
        CONSTRAINT [FK_stock_in_supplier] FOREIGN KEY ([supplier_id]) REFERENCES [dbo].[tbl_supplier]([supplier_id])
    );
    
    CREATE UNIQUE INDEX [IX_stock_in_number] ON [dbo].[tbl_stock_in] ([stock_in_number]);
    CREATE INDEX [IX_stock_in_po] ON [dbo].[tbl_stock_in] ([po_id]);
    CREATE INDEX [IX_stock_in_supplier] ON [dbo].[tbl_stock_in] ([supplier_id]);
    PRINT '✓ Created tbl_stock_in';
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_in already exists';
END
GO

-- ============================================
-- 11. Create tbl_stock_in_items
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_in_items] (
        [stock_in_items_id] INT IDENTITY(1,1) PRIMARY KEY,
        [stock_in_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity_received] INT NOT NULL,
        [unit_cost] DECIMAL(18,2) NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_in_items_stock_in] FOREIGN KEY ([stock_in_id]) REFERENCES [dbo].[tbl_stock_in]([stock_in_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_stock_in_items_product] FOREIGN KEY ([product_id]) REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_received] CHECK ([quantity_received] > 0),
        CONSTRAINT [CK_unit_cost_stock_in] CHECK ([unit_cost] >= 0)
    );
    
    CREATE INDEX [IX_stock_in_items_stock_in] ON [dbo].[tbl_stock_in_items] ([stock_in_id]);
    CREATE INDEX [IX_stock_in_items_product] ON [dbo].[tbl_stock_in_items] ([product_id]);
    PRINT '✓ Created tbl_stock_in_items';
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_in_items already exists';
END
GO

-- ============================================
-- 12. Create tbl_sales_order
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_sales_order] (
        [sales_order_id] INT IDENTITY(1,1) PRIMARY KEY,
        [user_id] INT NOT NULL,
        [order_number] NVARCHAR(50) NOT NULL,
        [order_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [total_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [notes] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [FK_sales_order_user] FOREIGN KEY ([user_id]) REFERENCES [dbo].[tbl_users]([user_id])
    );
    
    CREATE UNIQUE INDEX [IX_order_number] ON [dbo].[tbl_sales_order] ([order_number]);
    CREATE INDEX [IX_sales_order_user] ON [dbo].[tbl_sales_order] ([user_id]);
    CREATE INDEX [IX_sales_order_status] ON [dbo].[tbl_sales_order] ([status]);
    CREATE INDEX [IX_sales_order_date] ON [dbo].[tbl_sales_order] ([order_date]);
    PRINT '✓ Created tbl_sales_order';
END
ELSE
BEGIN
    PRINT '⚠ tbl_sales_order already exists';
END
GO

-- ============================================
-- 13. Create tbl_sales_order_items
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_sales_order_items] (
        [sales_order_item_id] INT IDENTITY(1,1) PRIMARY KEY,
        [sales_order_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity] INT NOT NULL,
        [unit_price] DECIMAL(18,2) NOT NULL,
        [subtotal] DECIMAL(18,2) NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_sales_order_items_sales_order] FOREIGN KEY ([sales_order_id]) REFERENCES [dbo].[tbl_sales_order]([sales_order_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_sales_order_items_product] FOREIGN KEY ([product_id]) REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_sales] CHECK ([quantity] > 0),
        CONSTRAINT [CK_unit_price] CHECK ([unit_price] >= 0)
    );
    
    CREATE INDEX [IX_sales_order_items_sales_order] ON [dbo].[tbl_sales_order_items] ([sales_order_id]);
    CREATE INDEX [IX_sales_order_items_product] ON [dbo].[tbl_sales_order_items] ([product_id]);
    PRINT '✓ Created tbl_sales_order_items';
END
ELSE
BEGIN
    PRINT '⚠ tbl_sales_order_items already exists';
END
GO

-- ============================================
-- 14. Create tbl_stock_out
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_out] (
        [stock_out_id] INT IDENTITY(1,1) PRIMARY KEY,
        [user_id] INT NOT NULL,
        [stock_out_number] NVARCHAR(50) NOT NULL,
        [out_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [reason] NVARCHAR(200) NULL,
        [status] NVARCHAR(50) NOT NULL DEFAULT 'Completed',
        [notes] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [FK_stock_out_user] FOREIGN KEY ([user_id]) REFERENCES [dbo].[tbl_users]([user_id])
    );
    
    CREATE UNIQUE INDEX [IX_stock_out_number] ON [dbo].[tbl_stock_out] ([stock_out_number]);
    CREATE INDEX [IX_stock_out_user] ON [dbo].[tbl_stock_out] ([user_id]);
    CREATE INDEX [IX_stock_out_date] ON [dbo].[tbl_stock_out] ([out_date]);
    PRINT '✓ Created tbl_stock_out';
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_out already exists';
END
GO

-- ============================================
-- 15. Create tbl_stock_out_items
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_out_items] (
        [stock_out_items_id] INT IDENTITY(1,1) PRIMARY KEY,
        [stock_out_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity] INT NOT NULL,
        [reason] NVARCHAR(200) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_out_items_stock_out] FOREIGN KEY ([stock_out_id]) REFERENCES [dbo].[tbl_stock_out]([stock_out_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_stock_out_items_product] FOREIGN KEY ([product_id]) REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_stock_out] CHECK ([quantity] > 0)
    );
    
    CREATE INDEX [IX_stock_out_items_stock_out] ON [dbo].[tbl_stock_out_items] ([stock_out_id]);
    CREATE INDEX [IX_stock_out_items_product] ON [dbo].[tbl_stock_out_items] ([product_id]);
    PRINT '✓ Created tbl_stock_out_items';
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_out_items already exists';
END
GO

PRINT '';
PRINT '=== All Tables Created Successfully! ===';
PRINT 'You can now sync your data from local database to cloud database.';
PRINT 'Go to Database Sync page and click "Sync to Cloud"';
GO





