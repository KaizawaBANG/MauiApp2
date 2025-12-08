# Why Do Identity Seeds Jump to 1000+? 🔍

## The Problem
SQL Server identity columns suddenly start generating IDs like 1006, 1007, etc., instead of continuing sequentially (8, 9, 10...).

## Root Causes

### 1. **Database Sync Operations** ⚠️ **MOST LIKELY CAUSE**
Your `DatabaseSyncService` uses `SET IDENTITY_INSERT ON` when syncing data from local to cloud database. This is the **most common cause** in your codebase.

**What happens:**
- When syncing, the service enables `IDENTITY_INSERT` to copy records with their original IDs
- If a record with ID 1006 is synced, SQL Server's identity seed gets set to 1006
- Future inserts then continue from 1007, 1008, etc.

**Location:** `Services/DatabaseSyncService.cs` (lines 171, 223)

### 2. **Manual Data Imports/Migrations**
If you've run scripts like:
- `SimpleSyncToCloud.sql` - Uses IDENTITY_INSERT
- `RenumberHighIds.sql` - Uses IDENTITY_INSERT
- `FixUserIdentityToContinueFrom4.sql` - Uses IDENTITY_INSERT

These scripts enable IDENTITY_INSERT to insert records with explicit IDs, which can cause the seed to jump.

### 3. **SQL Server Behavior After IDENTITY_INSERT**
When you use `SET IDENTITY_INSERT ON` and insert a record with an explicit ID:
- SQL Server updates the identity seed to that ID (or higher)
- Even if you delete the record later, the seed stays high
- This is by design to prevent ID conflicts

### 4. **Deleted Records Don't Reset Seeds**
If you delete records (e.g., delete product 1006), the identity seed **doesn't automatically reset**. It stays at 1006, so the next insert becomes 1007.

### 5. **Transaction Rollbacks**
If a transaction inserts a high ID and then rolls back:
- The record is deleted
- But the identity seed might still be incremented (depending on SQL Server version)

## Why 1000+ Specifically?

SQL Server has internal logic that sometimes jumps to round numbers (1000, 2000, etc.) when:
- There's a large gap in IDs
- After certain operations like IDENTITY_INSERT
- During database recovery or replication scenarios

## How to Prevent This

### ✅ **Best Practices:**

1. **Always Reset Seeds After IDENTITY_INSERT**
   ```sql
   SET IDENTITY_INSERT tbl_product ON;
   -- Insert records...
   SET IDENTITY_INSERT tbl_product OFF;
   
   -- ALWAYS reset the seed after IDENTITY_INSERT
   DECLARE @maxId INT;
   SELECT @maxId = ISNULL(MAX(product_id), 0) FROM tbl_product;
   DBCC CHECKIDENT ('tbl_product', RESEED, @maxId);
   ```

2. **Update DatabaseSyncService**
   After syncing, reset identity seeds:
   ```csharp
   // After sync completes
   var maxId = await GetMaxIdAsync(tableName);
   await ResetIdentitySeedAsync(tableName, maxId);
   ```

3. **Run Fix Scripts Periodically**
   - Run `FixIdentitySeedsForAllTables.sql` after any sync operations
   - Or schedule it to run automatically

4. **Avoid Manual ID Insertion**
   - Don't manually insert records with explicit IDs unless absolutely necessary
   - If you must, always reset the seed afterward

### 🔧 **Quick Fix Scripts:**

- **`FixIdentitySeedsForAllTables.sql`** - Fixes all tables at once
- **`CheckAndFixIdentityIssues.sql`** - Diagnoses issues first
- **`DeleteProduct1006.sql`** - Deletes high ID test records

## Example Scenario

**What happened in your case:**
1. Database sync ran and copied product with ID 1006
2. `IDENTITY_INSERT` was enabled during sync
3. SQL Server set identity seed to 1006
4. Next product insert got ID 1007 (instead of 9)
5. You noticed the jump and ran the fix script ✅

## Prevention Checklist

- [ ] After any database sync, run `FixIdentitySeedsForAllTables.sql`
- [ ] After using IDENTITY_INSERT in any script, reset the seed
- [ ] Monitor for high IDs periodically
- [ ] Consider adding seed reset to DatabaseSyncService automatically

## Summary

**The identity seed jumps because:**
1. Database sync operations use IDENTITY_INSERT
2. SQL Server updates the seed to match inserted IDs
3. The seed doesn't reset automatically when records are deleted
4. This is normal SQL Server behavior, but needs manual correction

**Solution:** Run `FixIdentitySeedsForAllTables.sql` regularly, especially after sync operations!





