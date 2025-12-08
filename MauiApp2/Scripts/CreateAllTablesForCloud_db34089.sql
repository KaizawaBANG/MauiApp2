-- Create All Tables for Cloud Database (db34089)
-- Run this script on your MonsterAPI cloud database to create all required tables
-- Tables are created in the correct order to respect foreign key dependencies
-- This script includes all columns to match your local database structure

USE [db34089];
GO

PRINT '=== Creating All Tables for Cloud Database (db34089) ===';
PRINT '';

-- ============================================
-- 1. Create tbl_roles
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_roles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_roles] (
        [role_id] INT IDENTITY(1,1) PRIMARY KEY,
        [role_name] VARCHAR(50) NOT NULL,
        [description] VARCHAR(255) NULL
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
        [username] VARCHAR(50) NOT NULL,
        [password_hash] VARCHAR(255) NOT NULL,
        [full_name] VARCHAR(100) NOT NULL,
        [email] VARCHAR(100) NOT NULL,
        [role_id] INT NOT NULL,
        [is_active] BIT NULL,
        [last_login] DATETIME NULL,
        [created_date] DATETIME NULL,
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
        [category_code] NVARCHAR(10) NOT NULL,
        [description] NVARCHAR(255) NULL
    );
    
    CREATE INDEX [IX_category_name] ON [dbo].[tbl_category] ([category_name]);
    CREATE UNIQUE INDEX [IX_category_code] ON [dbo].[tbl_category] ([category_code]);
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
        [brand_code] NVARCHAR(10) NOT NULL,
        [description] NVARCHAR(255) NULL
    );
    
    CREATE INDEX [IX_brand_name] ON [dbo].[tbl_brand] ([brand_name]);
    CREATE UNIQUE INDEX [IX_brand_code] ON [dbo].[tbl_brand] ([brand_code]);
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
        [product_name] NVARCHAR(255) NOT NULL,
        [product_sku] VARCHAR(50) NOT NULL,
        [model_number] VARCHAR(100) NULL,
        [cost_price] DECIMAL(10,2) NULL,
        [sell_price] DECIMAL(10,2) NOT NULL,
        [quantity] INT NULL DEFAULT 0,
        [status] BIT NULL DEFAULT 1,
        [created_date] DATETIME2(7) NULL DEFAULT GETDATE(),
        [modified_date] DATETIME2(7) NULL,
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
        [supplier_name] NVARCHAR(255) NOT NULL,
        [contact_number] VARCHAR(20) NULL,
        [email] VARCHAR(100) NULL,
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
        [supplier_id] INT NULL,
        [po_number] VARCHAR(50) NOT NULL,
        [order_date] DATE NULL,
        [expected_date] DATE NULL,
        [status] VARCHAR(20) NULL,
        [subtotal] DECIMAL(12,2) NULL,
        [tax_amount] DECIMAL(12,2) NULL DEFAULT 0,
        [total_amount] DECIMAL(12,2) NULL,
        [notes] NVARCHAR(1000) NULL,
        [cancellation_reason] NVARCHAR(100) NULL,
        [cancellation_remarks] NVARCHAR(1000) NULL,
        [created_date] DATETIME2(7) NULL,
        [modified_date] DATETIME2(7) NULL,
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
        [po_item_id] INT IDENTITY(1,1) PRIMARY KEY,
        [po_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity_ordered] INT NOT NULL,
        [unit_cost] DECIMAL(18,2) NOT NULL,
        [tax_rate] DECIMAL(5,2) NOT NULL DEFAULT 0,
        [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [subtotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [total] DECIMAL(18,2) NOT NULL DEFAULT 0,
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
-- 10. Create tbl_stock_in (with all columns)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_in] (
        [stock_in_id] INT IDENTITY(1,1) PRIMARY KEY,
        [po_id] INT NOT NULL,
        [supplier_id] INT NULL,
        [stock_in_number] NVARCHAR(50) NOT NULL,
        [stock_in_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [processed_by] INT NOT NULL,
        [notes] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_in_po] FOREIGN KEY ([po_id]) REFERENCES [dbo].[tbl_purchase_order]([po_id]),
        CONSTRAINT [FK_stock_in_supplier] FOREIGN KEY ([supplier_id]) REFERENCES [dbo].[tbl_supplier]([supplier_id]),
        CONSTRAINT [FK_stock_in_user] FOREIGN KEY ([processed_by]) REFERENCES [dbo].[tbl_users]([user_id])
    );
    
    CREATE UNIQUE INDEX [IX_stock_in_number] ON [dbo].[tbl_stock_in] ([stock_in_number]);
    CREATE INDEX [IX_stock_in_po] ON [dbo].[tbl_stock_in] ([po_id]);
    CREATE INDEX [IX_stock_in_supplier] ON [dbo].[tbl_stock_in] ([supplier_id]);
    CREATE INDEX [IX_stock_in_processed_by] ON [dbo].[tbl_stock_in] ([processed_by]);
    PRINT '✓ Created tbl_stock_in';
END
ELSE
BEGIN
    PRINT '⚠ tbl_stock_in already exists';
END
GO

-- ============================================
-- 11. Create tbl_stock_in_items (with all columns)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_in_items]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_in_items] (
        [stock_in_items_id] INT IDENTITY(1,1) PRIMARY KEY,
        [stock_in_id] INT NOT NULL,
        [product_id] INT NOT NULL,
        [quantity_received] INT NOT NULL,
        [quantity_rejected] INT NOT NULL DEFAULT 0,
        [unit_cost] DECIMAL(18,2) NOT NULL,
        [rejection_reason] NVARCHAR(100) NULL,
        [rejection_remarks] NVARCHAR(MAX) NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_in_items_stock_in] FOREIGN KEY ([stock_in_id]) REFERENCES [dbo].[tbl_stock_in]([stock_in_id]) ON DELETE CASCADE,
        CONSTRAINT [FK_stock_in_items_product] FOREIGN KEY ([product_id]) REFERENCES [dbo].[tbl_product]([product_id]),
        CONSTRAINT [CK_quantity_received] CHECK ([quantity_received] > 0),
        CONSTRAINT [CK_quantity_rejected] CHECK ([quantity_rejected] >= 0),
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
-- 12. Create tbl_sales_order (with all columns)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_sales_order]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_sales_order] (
        [sales_order_id] INT IDENTITY(1,1) PRIMARY KEY,
        [sales_order_number] NVARCHAR(50) NOT NULL,
        [sales_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [subtotal] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [total_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [payment_method] NVARCHAR(50) NOT NULL DEFAULT 'Cash',
        [processed_by] INT NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_sales_order_processed_by] FOREIGN KEY ([processed_by]) REFERENCES [dbo].[tbl_users]([user_id])
    );
    
    CREATE UNIQUE INDEX [IX_sales_order_number] ON [dbo].[tbl_sales_order] ([sales_order_number]);
    CREATE INDEX [IX_sales_order_processed_by] ON [dbo].[tbl_sales_order] ([processed_by]);
    PRINT '✓ Created tbl_sales_order';
END
ELSE
BEGIN
    PRINT '⚠ tbl_sales_order already exists';
END
GO

-- ============================================
-- 13. Create tbl_sales_order_items (with all columns)
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
        [tax_rate] DECIMAL(5,2) NOT NULL DEFAULT 0,
        [tax_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [total] DECIMAL(18,2) NOT NULL DEFAULT 0,
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
-- 14. Create tbl_stock_out (with all columns)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_stock_out]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_stock_out] (
        [stock_out_id] INT IDENTITY(1,1) PRIMARY KEY,
        [sales_order_id] INT NULL,
        [stock_out_number] NVARCHAR(50) NOT NULL,
        [stock_out_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [reason] NVARCHAR(50) NOT NULL,
        [processed_by] INT NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_stock_out_sales_order] FOREIGN KEY ([sales_order_id]) REFERENCES [dbo].[tbl_sales_order]([sales_order_id]),
        CONSTRAINT [FK_stock_out_processed_by] FOREIGN KEY ([processed_by]) REFERENCES [dbo].[tbl_users]([user_id])
    );
    
    CREATE UNIQUE INDEX [IX_stock_out_number] ON [dbo].[tbl_stock_out] ([stock_out_number]);
    CREATE INDEX [IX_stock_out_date] ON [dbo].[tbl_stock_out] ([stock_out_date]);
    CREATE INDEX [IX_stock_out_sales_order] ON [dbo].[tbl_stock_out] ([sales_order_id]);
    CREATE INDEX [IX_stock_out_processed_by] ON [dbo].[tbl_stock_out] ([processed_by]);
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
        [reason] NVARCHAR(50) NOT NULL,
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

-- ============================================
-- 16. Create tbl_chart_of_accounts
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_chart_of_accounts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_chart_of_accounts] (
        [account_id] INT IDENTITY(1,1) PRIMARY KEY,
        [account_code] VARCHAR(20) NOT NULL,
        [account_name] NVARCHAR(100) NOT NULL,
        [account_type] VARCHAR(20) NOT NULL, -- Asset, Liability, Equity, Revenue, Expense
        [description] NVARCHAR(255) NULL,
        [is_active] BIT NOT NULL DEFAULT 1,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [CK_account_type] CHECK ([account_type] IN ('Asset', 'Liability', 'Equity', 'Revenue', 'Expense'))
    );
    
    CREATE UNIQUE INDEX [IX_account_code] ON [dbo].[tbl_chart_of_accounts] ([account_code]);
    CREATE INDEX [IX_account_type] ON [dbo].[tbl_chart_of_accounts] ([account_type]);
    CREATE INDEX [IX_account_name] ON [dbo].[tbl_chart_of_accounts] ([account_name]);
    PRINT '✓ Created tbl_chart_of_accounts';
END
ELSE
BEGIN
    PRINT '⚠ tbl_chart_of_accounts already exists';
END
GO

-- ============================================
-- 17. Create tbl_general_ledger
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_general_ledger]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_general_ledger] (
        [ledger_id] INT IDENTITY(1,1) PRIMARY KEY,
        [transaction_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [account_id] INT NOT NULL,
        [debit_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [credit_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [description] NVARCHAR(500) NOT NULL,
        [reference_type] VARCHAR(50) NULL, -- Sales, Purchase, Payment, Expense, StockIn, StockOut, Manual
        [reference_id] INT NULL, -- Links to sales_order_id, po_id, expense_id, etc.
        [created_by] INT NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_general_ledger_account] FOREIGN KEY ([account_id]) REFERENCES [dbo].[tbl_chart_of_accounts]([account_id]),
        CONSTRAINT [FK_general_ledger_user] FOREIGN KEY ([created_by]) REFERENCES [dbo].[tbl_users]([user_id]),
        CONSTRAINT [CK_debit_credit] CHECK (([debit_amount] > 0 AND [credit_amount] = 0) OR ([debit_amount] = 0 AND [credit_amount] > 0))
    );
    
    CREATE INDEX [IX_general_ledger_account] ON [dbo].[tbl_general_ledger] ([account_id]);
    CREATE INDEX [IX_general_ledger_date] ON [dbo].[tbl_general_ledger] ([transaction_date]);
    CREATE INDEX [IX_general_ledger_reference] ON [dbo].[tbl_general_ledger] ([reference_type], [reference_id]);
    CREATE INDEX [IX_general_ledger_created_by] ON [dbo].[tbl_general_ledger] ([created_by]);
    PRINT '✓ Created tbl_general_ledger';
END
ELSE
BEGIN
    PRINT '⚠ tbl_general_ledger already exists';
END
GO

-- ============================================
-- 18. Create tbl_accounts_payable
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_accounts_payable]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_accounts_payable] (
        [ap_id] INT IDENTITY(1,1) PRIMARY KEY,
        [po_id] INT NULL,
        [supplier_id] INT NOT NULL,
        [invoice_number] NVARCHAR(100) NULL,
        [total_amount] DECIMAL(18,2) NOT NULL,
        [paid_amount] DECIMAL(18,2) NOT NULL DEFAULT 0,
        [balance_amount] AS ([total_amount] - [paid_amount]) PERSISTED,
        [due_date] DATE NULL,
        [status] VARCHAR(20) NOT NULL DEFAULT 'Unpaid', -- Unpaid, Partial, Paid
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [FK_accounts_payable_po] FOREIGN KEY ([po_id]) REFERENCES [dbo].[tbl_purchase_order]([po_id]),
        CONSTRAINT [FK_accounts_payable_supplier] FOREIGN KEY ([supplier_id]) REFERENCES [dbo].[tbl_supplier]([supplier_id]),
        CONSTRAINT [CK_accounts_payable_amount] CHECK ([total_amount] > 0),
        CONSTRAINT [CK_accounts_payable_paid] CHECK ([paid_amount] >= 0 AND [paid_amount] <= [total_amount]),
        CONSTRAINT [CK_accounts_payable_status] CHECK ([status] IN ('Unpaid', 'Partial', 'Paid'))
    );
    
    CREATE INDEX [IX_accounts_payable_po] ON [dbo].[tbl_accounts_payable] ([po_id]);
    CREATE INDEX [IX_accounts_payable_supplier] ON [dbo].[tbl_accounts_payable] ([supplier_id]);
    CREATE INDEX [IX_accounts_payable_status] ON [dbo].[tbl_accounts_payable] ([status]);
    CREATE INDEX [IX_accounts_payable_due_date] ON [dbo].[tbl_accounts_payable] ([due_date]);
    PRINT '✓ Created tbl_accounts_payable';
END
ELSE
BEGIN
    PRINT '⚠ tbl_accounts_payable already exists';
END
GO

-- ============================================
-- 19. Create tbl_payments
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_payments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_payments] (
        [payment_id] INT IDENTITY(1,1) PRIMARY KEY,
        [ap_id] INT NULL, -- Links to accounts payable
        [payment_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [amount] DECIMAL(18,2) NOT NULL,
        [payment_method] NVARCHAR(50) NOT NULL, -- Cash, Card, GCash, PayMaya, Bank Transfer
        [reference_number] NVARCHAR(100) NULL, -- Check number, transaction number, etc.
        [notes] NVARCHAR(500) NULL,
        [created_by] INT NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_payments_ap] FOREIGN KEY ([ap_id]) REFERENCES [dbo].[tbl_accounts_payable]([ap_id]),
        CONSTRAINT [FK_payments_user] FOREIGN KEY ([created_by]) REFERENCES [dbo].[tbl_users]([user_id]),
        CONSTRAINT [CK_payment_amount] CHECK ([amount] > 0)
    );
    
    CREATE INDEX [IX_payments_ap] ON [dbo].[tbl_payments] ([ap_id]);
    CREATE INDEX [IX_payments_date] ON [dbo].[tbl_payments] ([payment_date]);
    CREATE INDEX [IX_payments_created_by] ON [dbo].[tbl_payments] ([created_by]);
    PRINT '✓ Created tbl_payments';
END
ELSE
BEGIN
    PRINT '⚠ tbl_payments already exists';
END
GO

-- ============================================
-- 20. Create tbl_expenses
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tbl_expenses]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tbl_expenses] (
        [expense_id] INT IDENTITY(1,1) PRIMARY KEY,
        [expense_date] DATE NOT NULL,
        [category] NVARCHAR(100) NOT NULL, -- Rent, Utilities, Salaries, Supplies, etc.
        [description] NVARCHAR(500) NOT NULL,
        [amount] DECIMAL(18,2) NOT NULL,
        [payment_method] NVARCHAR(50) NOT NULL, -- Cash, Card, GCash, PayMaya, Bank Transfer
        [reference_number] NVARCHAR(100) NULL, -- Receipt number, invoice number, etc.
        [created_by] INT NOT NULL,
        [created_date] DATETIME NOT NULL DEFAULT GETDATE(),
        [modified_date] DATETIME NULL,
        CONSTRAINT [FK_expenses_user] FOREIGN KEY ([created_by]) REFERENCES [dbo].[tbl_users]([user_id]),
        CONSTRAINT [CK_expense_amount] CHECK ([amount] > 0)
    );
    
    CREATE INDEX [IX_expenses_date] ON [dbo].[tbl_expenses] ([expense_date]);
    CREATE INDEX [IX_expenses_category] ON [dbo].[tbl_expenses] ([category]);
    CREATE INDEX [IX_expenses_created_by] ON [dbo].[tbl_expenses] ([created_by]);
    PRINT '✓ Created tbl_expenses';
END
ELSE
BEGIN
    PRINT '⚠ tbl_expenses already exists';
END
GO

PRINT '';
PRINT '=== All Tables Created Successfully! ===';
PRINT 'Database: db34089';
PRINT 'Accounting tables included:';
PRINT '  - tbl_chart_of_accounts';
PRINT '  - tbl_general_ledger';
PRINT '  - tbl_accounts_payable';
PRINT '  - tbl_payments';
PRINT '  - tbl_expenses';
PRINT 'You can now sync your data from local database to cloud database.';
PRINT 'Go to Database Sync page and click "Sync to Cloud"';
GO

