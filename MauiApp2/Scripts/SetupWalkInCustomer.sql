-- Setup Walk-in Customer
-- This script deletes all existing customer data and creates a "Walk-in Customer" record
-- All sales orders will be updated to use the walk-in customer

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Temporarily disable the foreign key constraint
    ALTER TABLE tbl_sales_order NOCHECK CONSTRAINT FK_sales_order_customer;
    PRINT 'Temporarily disabled foreign key constraint.';

    -- Step 2: Update ALL sales orders to use customer_id = 1 (walk-in customer)
    -- This includes both NULL and existing customer_id values
    UPDATE tbl_sales_order
    SET customer_id = 1;
    
    DECLARE @UpdatedRows INT = @@ROWCOUNT;
    PRINT 'Updated ' + CAST(@UpdatedRows AS VARCHAR(10)) + ' sales orders to use customer_id = 1.';

    -- Step 3: Delete all existing customers
    DELETE FROM tbl_customer;
    DECLARE @DeletedRows INT = @@ROWCOUNT;
    PRINT 'Deleted ' + CAST(@DeletedRows AS VARCHAR(10)) + ' existing customers.';

    -- Step 4: Reset the identity seed to start from 1
    DBCC CHECKIDENT ('tbl_customer', RESEED, 0);
    PRINT 'Reset identity seed.';

    -- Step 5: Create the Walk-in Customer (will get customer_id = 1)
    SET IDENTITY_INSERT tbl_customer ON;
    
    INSERT INTO tbl_customer (customer_id, customer_name, contact_number, email, address, is_active, created_date)
    VALUES (1, 'Walk-in Customer', NULL, NULL, NULL, 1, GETDATE());
    
    SET IDENTITY_INSERT tbl_customer OFF;
    PRINT 'Created Walk-in Customer with customer_id = 1.';

    -- Step 6: Re-enable the foreign key constraint
    ALTER TABLE tbl_sales_order CHECK CONSTRAINT FK_sales_order_customer;
    PRINT 'Re-enabled foreign key constraint.';

    COMMIT TRANSACTION;
    PRINT 'Walk-in Customer setup completed successfully!';
END TRY
BEGIN CATCH
    -- Re-enable constraint in case of error
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_sales_order_customer' AND is_disabled = 1)
    BEGIN
        ALTER TABLE tbl_sales_order CHECK CONSTRAINT FK_sales_order_customer;
    END
    
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO

