-- Add Simple User Roles to tbl_roles table
-- This script adds simplified roles for the inventory management system
-- Note: Inventory Manager already handles warehouse operations, so no separate Warehouse Staff role needed

-- Cashier (rename Sales Staff if it exists, otherwise add Cashier)
IF EXISTS (SELECT 1 FROM [DB_QuadTech].[dbo].[tbl_roles] WHERE role_name = 'Sales Staff')
BEGIN
    UPDATE [DB_QuadTech].[dbo].[tbl_roles]
    SET role_name = 'Cashier', description = 'Handles sales transactions'
    WHERE role_name = 'Sales Staff';
    PRINT 'Sales Staff role renamed to Cashier successfully';
END
ELSE IF NOT EXISTS (SELECT 1 FROM [DB_QuadTech].[dbo].[tbl_roles] WHERE role_name = 'Cashier')
BEGIN
    INSERT INTO [DB_QuadTech].[dbo].[tbl_roles] (role_name, description)
    VALUES ('Cashier', 'Handles sales transactions');
    PRINT 'Cashier role added successfully';
END
ELSE
BEGIN
    PRINT 'Cashier role already exists';
END
GO

-- Accountant
IF NOT EXISTS (SELECT 1 FROM [DB_QuadTech].[dbo].[tbl_roles] WHERE role_name = 'Accountant')
BEGIN
    INSERT INTO [DB_QuadTech].[dbo].[tbl_roles] (role_name, description)
    VALUES ('Accountant', 'Handles accounting operations');
    PRINT 'Accountant role added successfully';
END
ELSE
BEGIN
    PRINT 'Accountant role already exists';
END
GO

-- Display all roles
SELECT role_id, role_name, description 
FROM [DB_QuadTech].[dbo].[tbl_roles]
ORDER BY role_id;
GO

