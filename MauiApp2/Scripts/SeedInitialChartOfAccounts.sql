-- Seed Initial Chart of Accounts
-- Run this script after creating the accounting tables
-- This creates basic accounts needed for the accounting system

USE [db34089];
GO

PRINT '=== Seeding Initial Chart of Accounts ===';
PRINT '';

-- Check if accounts already exist
IF EXISTS (SELECT * FROM tbl_chart_of_accounts WHERE account_code = '1001')
BEGIN
    PRINT '⚠ Chart of Accounts already seeded. Skipping...';
END
ELSE
BEGIN
    -- ASSETS (1000-1999)
    PRINT 'Creating Asset Accounts...';

    INSERT INTO tbl_chart_of_accounts (account_code, account_name, account_type, description, is_active, created_date)
    VALUES 
        ('1001', 'Cash', 'Asset', 'Cash on hand and in bank accounts', 1, GETDATE()),
        ('1002', 'Inventory', 'Asset', 'Products available for sale', 1, GETDATE()),
        ('1003', 'Accounts Receivable', 'Asset', 'Money owed by customers (if credit sales)', 1, GETDATE());

    PRINT '✓ Created Asset Accounts';

    -- LIABILITIES (2000-2999)
    PRINT 'Creating Liability Accounts...';

    INSERT INTO tbl_chart_of_accounts (account_code, account_name, account_type, description, is_active, created_date)
    VALUES 
        ('2001', 'Accounts Payable', 'Liability', 'Money owed to suppliers', 1, GETDATE());

    PRINT '✓ Created Liability Accounts';

    -- EQUITY (3000-3999)
    PRINT 'Creating Equity Accounts...';

    INSERT INTO tbl_chart_of_accounts (account_code, account_name, account_type, description, is_active, created_date)
    VALUES 
        ('3001', 'Owner''s Equity', 'Equity', 'Owner investment in the business', 1, GETDATE()),
        ('3002', 'Retained Earnings', 'Equity', 'Accumulated profits kept in business', 1, GETDATE());

    PRINT '✓ Created Equity Accounts';

    -- REVENUE (4000-4999)
    PRINT 'Creating Revenue Accounts...';

    INSERT INTO tbl_chart_of_accounts (account_code, account_name, account_type, description, is_active, created_date)
    VALUES 
        ('4001', 'Sales Revenue', 'Revenue', 'Revenue from product sales', 1, GETDATE()),
        ('4002', 'Other Income', 'Revenue', 'Other sources of income', 1, GETDATE());

    PRINT '✓ Created Revenue Accounts';

    -- EXPENSES (5000-5999)
    PRINT 'Creating Expense Accounts...';

    INSERT INTO tbl_chart_of_accounts (account_code, account_name, account_type, description, is_active, created_date)
    VALUES 
        ('5001', 'Cost of Goods Sold', 'Expense', 'Direct cost of products sold', 1, GETDATE()),
        ('5002', 'Rent Expense', 'Expense', 'Monthly rent payments', 1, GETDATE()),
        ('5003', 'Utilities Expense', 'Expense', 'Electricity, water, internet, etc.', 1, GETDATE()),
        ('5004', 'Salaries Expense', 'Expense', 'Employee wages and salaries', 1, GETDATE()),
        ('5005', 'Supplies Expense', 'Expense', 'Office supplies and materials', 1, GETDATE()),
        ('5006', 'Marketing Expense', 'Expense', 'Advertising and marketing costs', 1, GETDATE()),
        ('5007', 'Other Expenses', 'Expense', 'Other operational expenses', 1, GETDATE());

    PRINT '✓ Created Expense Accounts';

    PRINT '';
    PRINT '=== Chart of Accounts Seeded Successfully! ===';
    PRINT 'Total Accounts Created: 13';
    PRINT '';
    PRINT 'Asset Accounts: 3';
    PRINT 'Liability Accounts: 1';
    PRINT 'Equity Accounts: 2';
    PRINT 'Revenue Accounts: 2';
    PRINT 'Expense Accounts: 5';
    PRINT '';
    PRINT 'You can now use these accounts in your accounting system.';
END
GO

