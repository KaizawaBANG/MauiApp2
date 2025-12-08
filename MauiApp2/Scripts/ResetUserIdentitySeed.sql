-- Reset IDENTITY seed for tbl_users to continue from the highest existing user_id
-- This fixes the issue where new users get IDs like 1003 instead of sequential numbers like 5

-- Check current maximum user_id
DECLARE @MaxUserId INT;
SELECT @MaxUserId = ISNULL(MAX(user_id), 0) FROM tbl_users;

-- Reset IDENTITY seed to continue from the next number after the maximum
-- If max is 4, next ID will be 5
DBCC CHECKIDENT ('tbl_users', RESEED, @MaxUserId);

PRINT 'IDENTITY seed reset. Maximum user_id is: ' + CAST(@MaxUserId AS VARCHAR(10));
PRINT 'Next new user will have user_id: ' + CAST(@MaxUserId + 1 AS VARCHAR(10));


