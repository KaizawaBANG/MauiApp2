-- Simple Database Sync Script
-- Run this script while connected to your LOCAL database
-- Make sure to update the connection strings below

-- Step 1: First, create this as a stored procedure or run it in a query window
-- You'll need to connect to CLOUD database and run the INSERT statements

-- IMPORTANT: Connect to your CLOUD database first, then run the sync script below
-- This script assumes you've already created all tables in the cloud database

USE [db33496]; -- Change to your cloud database name
GO

-- Enable IDENTITY_INSERT for tables that need it
SET IDENTITY_INSERT [dbo].[tbl_roles] ON;
GO

-- Copy Roles (must be first - referenced by users)
INSERT INTO [dbo].[tbl_roles] ([role_id], [role_name], [role_description], [is_active], [created_date])
SELECT [role_id], [role_name], [role_description], [is_active], [created_date]
FROM [LOCAL_SERVER].DB_QuadTech.dbo.tbl_roles
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[tbl_roles] WHERE role_id = [LOCAL_SERVER].DB_QuadTech.dbo.tbl_roles.role_id
);
GO

SET IDENTITY_INSERT [dbo].[tbl_roles] OFF;
GO

-- Copy Users
SET IDENTITY_INSERT [dbo].[tbl_users] ON;
GO

INSERT INTO [dbo].[tbl_users] ([user_id], [role_id], [username], [email], [password_hash], [full_name], [is_active], [last_login], [created_date])
SELECT [user_id], [role_id], [username], [email], [password_hash], [full_name], [is_active], [last_login], [created_date]
FROM [LOCAL_SERVER].DB_QuadTech.dbo.tbl_users
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[tbl_users] WHERE user_id = [LOCAL_SERVER].DB_QuadTech.dbo.tbl_users.user_id
);
GO

SET IDENTITY_INSERT [dbo].[tbl_users] OFF;
GO

-- Continue with other tables...

PRINT 'Sync completed!';
GO







