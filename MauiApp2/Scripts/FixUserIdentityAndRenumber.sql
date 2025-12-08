-- Fix User IDENTITY seed and renumber user_id 1003 to 5
-- This will make the numbering sequential: 1, 2, 3, 4, 5, then next will be 6

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Renumber user_id 1003 to 5 (if it exists)
    -- First, check if user_id 5 already exists
    IF EXISTS (SELECT 1 FROM tbl_users WHERE user_id = 1003)
    BEGIN
        IF EXISTS (SELECT 1 FROM tbl_users WHERE user_id = 5)
        BEGIN
            -- If user_id 5 already exists, we need to use a temporary ID first
            PRINT 'User ID 5 already exists. Using temporary ID...';
            
            -- Temporarily move user_id 1003 to a safe temporary ID (e.g., 9999)
            UPDATE tbl_users SET user_id = 9999 WHERE user_id = 1003;
            
            -- If there was a conflict, move the existing user 5 to 9998
            IF EXISTS (SELECT 1 FROM tbl_users WHERE user_id = 5)
            BEGIN
                UPDATE tbl_users SET user_id = 9998 WHERE user_id = 5;
            END
            
            -- Now move the user from 9999 to 5
            UPDATE tbl_users SET user_id = 5 WHERE user_id = 9999;
            
            -- Move back the original user 5 if it existed
            IF EXISTS (SELECT 1 FROM tbl_users WHERE user_id = 9998)
            BEGIN
                -- Find next available ID after max
                DECLARE @NextId INT;
                SELECT @NextId = ISNULL(MAX(user_id), 0) + 1 FROM tbl_users WHERE user_id < 9998;
                UPDATE tbl_users SET user_id = @NextId WHERE user_id = 9998;
            END
        END
        ELSE
        BEGIN
            -- Simply renumber 1003 to 5
            UPDATE tbl_users SET user_id = 5 WHERE user_id = 1003;
        END
        
        PRINT 'User renumbered from 1003 to 5 successfully.';
    END
    ELSE
    BEGIN
        PRINT 'User ID 1003 does not exist. Skipping renumbering.';
    END

    -- Step 2: Reset IDENTITY seed to continue from the maximum user_id
    DECLARE @MaxUserId INT;
    SELECT @MaxUserId = ISNULL(MAX(user_id), 0) FROM tbl_users WHERE user_id < 9999; -- Exclude temporary IDs
    
    DBCC CHECKIDENT ('tbl_users', RESEED, @MaxUserId);
    
    PRINT 'IDENTITY seed reset. Maximum user_id is: ' + CAST(@MaxUserId AS VARCHAR(10));
    PRINT 'Next new user will have user_id: ' + CAST(@MaxUserId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
    PRINT 'Transaction completed successfully!';
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred. Transaction rolled back.';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH;


