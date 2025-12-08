# How to Revert Database Cleanup

## Overview
If you need to undo the database cleanup and restore your data, you have **one option**: restore from a backup.

## ⚠️ Important
**Once data is deleted, it's gone forever unless you have a backup!**

There is NO way to recover deleted data without a backup. This is why backups are critical.

---

## Method 1: Restore from Backup (Recommended)

### Step 1: Find Your Backup File
Look for your backup file (`.bak` file) in:
- `C:\Backup\` (default location)
- Or wherever you saved it

Backup files are named like:
- `YourDatabaseName_Backup_20250115_120000.bak`
- Format: `DatabaseName_Backup_YYYYMMDD_HHMMSS.bak`

### Step 2: Use Restore Script
1. Open `RestoreDatabaseFromBackup.sql`
2. Update the `@BackupFilePath` variable with your backup file path:
   ```sql
   DECLARE @BackupFilePath NVARCHAR(260) = 'C:\Backup\YourDatabaseName_Backup_20250115_120000.bak';
   ```
3. Execute the script (F5)

### Step 3: Verify Restore
After restore, verify:
- ✅ All your data is back
- ✅ Sales orders are restored
- ✅ Purchase orders are restored
- ✅ All transactions are back

---

## Method 2: Using SQL Server Management Studio (SSMS)

### Step 1: Right-click Database
1. Open SQL Server Management Studio
2. Connect to your server
3. Expand "Databases"
4. Right-click your database
5. Select "Tasks" → "Restore" → "Database..."

### Step 2: Configure Restore
1. Select "Device"
2. Click "..." to browse
3. Add your backup file (`.bak`)
4. Click "OK"

### Step 3: Options Tab
1. Go to "Options" tab
2. Check "Overwrite the existing database"
3. Click "OK" to restore

---

## Method 3: Automatic Backup Before Cleanup

### Use the Combined Script
Run `CleanDatabaseWithBackup.sql` instead of `CleanDatabaseKeepEssentialData.sql`

This script:
1. ✅ Creates backup automatically
2. ✅ Performs cleanup
3. ✅ Shows you the backup file location

**This is the SAFEST option!**

---

## What If You Don't Have a Backup?

### ❌ Bad News
If you don't have a backup, **you cannot recover the deleted data**.

### ✅ Good News
- Essential data (Users, Products, etc.) is still there
- You can start fresh with new transactions
- System structure is intact

### Prevention
**Always create a backup before cleanup!**

Use:
- `BackupDatabaseBeforeCleanup.sql` - Manual backup
- `CleanDatabaseWithBackup.sql` - Automatic backup

---

## Quick Reference

### Create Backup
```sql
-- Run: BackupDatabaseBeforeCleanup.sql
-- Or use: CleanDatabaseWithBackup.sql (includes backup)
```

### Restore Backup
```sql
-- Run: RestoreDatabaseFromBackup.sql
-- Update @BackupFilePath variable first
```

### Check Backup Files
```sql
-- List all backup files in folder
EXEC xp_cmdshell 'dir C:\Backup\*.bak';
```

---

## Best Practices

1. **Always backup before cleanup**
   - Use `BackupDatabaseBeforeCleanup.sql`
   - Or use `CleanDatabaseWithBackup.sql`

2. **Keep multiple backups**
   - Don't delete old backups immediately
   - Keep at least 2-3 recent backups

3. **Test your backups**
   - Periodically test restoring from backup
   - Make sure backups are valid

4. **Document backup locations**
   - Write down where backups are stored
   - Label backup files clearly

---

## Troubleshooting

### "Backup file not found"
- Check the file path is correct
- Make sure the backup file exists
- Verify file permissions

### "Database is in use"
- Close all connections to the database
- Stop the application
- Try restore again

### "Restore failed"
- Check backup file is not corrupted
- Verify you have sufficient disk space
- Check SQL Server permissions

---

## Summary

✅ **To Revert:** Restore from backup  
✅ **To Backup:** Use `BackupDatabaseBeforeCleanup.sql`  
✅ **Safest Option:** Use `CleanDatabaseWithBackup.sql`  
❌ **No Backup = No Recovery**

**Remember: Prevention is better than cure! Always backup first!**




