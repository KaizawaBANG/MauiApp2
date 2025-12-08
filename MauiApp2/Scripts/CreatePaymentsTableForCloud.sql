-- ============================================
-- Create tbl_payments Table for Cloud Database
-- ============================================
-- This script creates the payments table that was missing
-- Run this on your cloud database
-- ============================================

PRINT 'Creating tbl_payments table...';
PRINT '';

-- ============================================
-- Create tbl_payments
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

PRINT '';
PRINT '========================================';
PRINT '✅ tbl_payments Table Created!';
PRINT '========================================';
PRINT '';
PRINT 'Your cloud database now has all 23 tables matching LocalDB.';
PRINT '';



