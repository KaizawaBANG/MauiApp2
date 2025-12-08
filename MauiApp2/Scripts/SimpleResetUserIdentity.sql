-- Simple Fix: Reset IDENTITY seed to 4 so next user will be 5
-- WARNING: This will leave user_id 1003 as is, creating a gap in the sequence
-- Use this if you want to keep user 1003 with its current ID

-- Find the maximum user_id (excluding 1003 if you want sequential from 4)
DECLARE @MaxUserId INT;
SELECT @MaxUserId = ISNULL(MAX(user_id), 0) 
FROM tbl_users 
WHERE user_id < 1000; -- Only consider normal sequential IDs

-- If no normal IDs found, default to 4
IF @MaxUserId = 0
    SET @MaxUserId = 4;

-- Reset IDENTITY seed
DBCC CHECKIDENT ('tbl_users', RESEED, @MaxUserId);

PRINT 'IDENTITY seed reset to: ' + CAST(@MaxUserId AS VARCHAR(10));
PRINT 'Next new user will have user_id: ' + CAST(@MaxUserId + 1 AS VARCHAR(10));
PRINT '';
PRINT 'NOTE: User ID 1003 will remain as is. To fix this, run FixUserIdentityAndRenumber.sql instead.';


