-- SIMPLE FIX: Delete user 1003, then reset IDENTITY seed to 4
-- Next new user will be 5
-- WARNING: This will DELETE the user with ID 1003 (Chan Anito)
-- Only use this if you want to remove that user permanently

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Deleting user with ID 1003...';
    
    -- Delete the user
    DELETE FROM tbl_users WHERE user_id = 1003;
    
    PRINT 'User deleted. Resetting IDENTITY seed...';
    
    -- Reset IDENTITY seed to 4 (so next will be 5)
    DECLARE @MaxUserId INT;
    SELECT @MaxUserId = ISNULL(MAX(user_id), 0) FROM tbl_users;
    
    DBCC CHECKIDENT ('tbl_users', RESEED, @MaxUserId);
    
    PRINT 'IDENTITY seed reset to: ' + CAST(@MaxUserId AS VARCHAR(10));
    PRINT 'Next new user will have user_id: ' + CAST(@MaxUserId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
    PRINT '';
    PRINT '✅ Completed! User 1003 deleted and IDENTITY reset.';
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '❌ Error occurred. Transaction rolled back.';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH;


