-- Fix User IDENTITY: Renumber user 1003 to 5, then reset seed to continue sequentially
-- This ensures next user will be 6 after renumbering 1003 to 5
-- IMPORTANT: We cannot UPDATE identity columns, so we DELETE and INSERT instead

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting user IDENTITY fix...';
    PRINT '';
    
    -- Step 1: Check if user_id 1003 exists
    IF NOT EXISTS (SELECT 1 FROM tbl_users WHERE user_id = 1003)
    BEGIN
        PRINT 'User ID 1003 does not exist. Nothing to renumber.';
        GOTO ResetSeed;
    END
    
    -- Step 2: Get all data from user 1003
    DECLARE @Username NVARCHAR(100), @Email NVARCHAR(100), @FullName NVARCHAR(100), 
            @PasswordHash NVARCHAR(255), @RoleId INT, @IsActive BIT, 
            @LastLogin DATETIME, @CreatedDate DATETIME;
    
    SELECT @Username = username, @Email = email, @FullName = full_name,
           @PasswordHash = password_hash, @RoleId = role_id, @IsActive = is_active,
           @LastLogin = last_login, @CreatedDate = created_date
    FROM tbl_users WHERE user_id = 1003;
    
    PRINT 'Found user 1003: ' + @FullName + ' (' + @Username + ')';
    
    -- Step 3: Check if user_id 5 already exists
    IF EXISTS (SELECT 1 FROM tbl_users WHERE user_id = 5)
    BEGIN
        PRINT 'User ID 5 already exists. Moving it to ID 6 first...';
        
        -- Get data from user 5
        DECLARE @User5Username NVARCHAR(100), @User5Email NVARCHAR(100), @User5FullName NVARCHAR(100), 
                @User5PasswordHash NVARCHAR(255), @User5RoleId INT, @User5IsActive BIT, 
                @User5LastLogin DATETIME, @User5CreatedDate DATETIME;
        
        SELECT @User5Username = username, @User5Email = email, @User5FullName = full_name,
               @User5PasswordHash = password_hash, @User5RoleId = role_id, @User5IsActive = is_active,
               @User5LastLogin = last_login, @User5CreatedDate = created_date
        FROM tbl_users WHERE user_id = 5;
        
        -- Delete user 5
        DELETE FROM tbl_users WHERE user_id = 5;
        
        -- Enable IDENTITY_INSERT to insert with explicit ID
        SET IDENTITY_INSERT tbl_users ON;
        
        -- Insert user 5 as user 6
        INSERT INTO tbl_users (user_id, username, email, password_hash, full_name, 
                              role_id, is_active, last_login, created_date)
        VALUES (6, @User5Username, @User5Email, @User5PasswordHash, @User5FullName, 
                @User5RoleId, @User5IsActive, @User5LastLogin, @User5CreatedDate);
        
        SET IDENTITY_INSERT tbl_users OFF;
        
        PRINT 'User 5 moved to ID 6.';
    END
    
    -- Step 4: Delete user 1003
    PRINT 'Deleting user 1003...';
    DELETE FROM tbl_users WHERE user_id = 1003;
    
    -- Step 5: Enable IDENTITY_INSERT and insert user as ID 5
    SET IDENTITY_INSERT tbl_users ON;
    
    INSERT INTO tbl_users (user_id, username, email, password_hash, full_name, 
                          role_id, is_active, last_login, created_date)
    VALUES (5, @Username, @Email, @PasswordHash, @FullName, 
            @RoleId, @IsActive, @LastLogin, @CreatedDate);
    
    SET IDENTITY_INSERT tbl_users OFF;
    
    PRINT 'User renumbered from 1003 to 5 successfully.';
    PRINT '';
    
ResetSeed:
    -- Step 6: Reset IDENTITY seed to continue from maximum
    DECLARE @MaxUserId INT;
    SELECT @MaxUserId = ISNULL(MAX(user_id), 0) FROM tbl_users;
    
    DBCC CHECKIDENT ('tbl_users', RESEED, @MaxUserId);
    
    PRINT 'IDENTITY seed reset completed!';
    PRINT 'Maximum user_id: ' + CAST(@MaxUserId AS VARCHAR(10));
    PRINT 'Next new user will have user_id: ' + CAST(@MaxUserId + 1 AS VARCHAR(10));
    
    COMMIT TRANSACTION;
    PRINT '';
    PRINT '✅ Transaction completed successfully!';
    
END TRY
BEGIN CATCH
    -- Make sure to turn off IDENTITY_INSERT even on error
    IF (SELECT OBJECTPROPERTY(OBJECT_ID('tbl_users'), 'TableHasIdentity')) = 1
    BEGIN
        BEGIN TRY
            SET IDENTITY_INSERT tbl_users OFF;
        END TRY
        BEGIN CATCH
            -- Ignore error if already off
        END CATCH
    END
    
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '❌ Error occurred. Transaction rolled back.';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
END CATCH;
