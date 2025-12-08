-- ============================================
-- Test Cloud Database Connection
-- ============================================
-- Run this simple test to verify connectivity
-- ============================================

PRINT 'Testing cloud database connection...';
PRINT '';

-- Simple connection test
SELECT 
    'Connection Successful!' AS Status,
    GETDATE() AS CurrentTime,
    DB_NAME() AS DatabaseName,
    @@VERSION AS SQLServerVersion;

PRINT '';
PRINT 'If you see this message, your connection is working!';
PRINT 'You can proceed with running CompleteCloudDatabaseSetup.sql';



