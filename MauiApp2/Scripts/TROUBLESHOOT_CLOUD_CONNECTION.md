# Troubleshooting Cloud Database Connection Issues

## Error: "A transport-level error has occurred"

This error indicates a network connectivity problem. Here are solutions:

## Solution 1: Check Connection String Settings

Update your connection string to include timeout settings:

```
Data Source=db34089.public.databaseasp.net,1433;
Initial Catalog=db34089;
User ID=db34089;
Password=9Ch?b3!B2Sm%;
Trust Server Certificate=True;
Connection Timeout=60;
Command Timeout=300;
```

## Solution 2: Test Connection First

1. In SSMS, try to connect manually:
   - Server name: `db34089.public.databaseasp.net,1433`
   - Authentication: SQL Server Authentication
   - Login: `db34089`
   - Password: `9Ch?b3!B2Sm%`
   - Check "Trust server certificate"

2. If connection works manually, the script should work too.

## Solution 3: Run Script in Smaller Batches

The script might be timing out because it's too large. Try running it in sections:

### Batch 1: Core Tables (Run First)
```sql
-- Run sections 1-7 (roles, users, category, brand, tax, product, supplier)
-- Stop after tbl_supplier is created
```

### Batch 2: Transaction Tables
```sql
-- Run sections 8-15 (purchase orders, stock in/out, sales orders)
```

### Batch 3: Accounting Tables
```sql
-- Run sections 16-19 (chart of accounts, GL, AP, expenses)
```

### Batch 4: Sync Tables
```sql
-- Run sections 20-22 (sync queue, sync history, connectivity log)
```

### Batch 5: Initial Data
```sql
-- Run the INSERT statements for roles and taxes
```

## Solution 4: Check Firewall/Network

1. **Check if port 1433 is open:**
   - Some networks block SQL Server port 1433
   - Try from a different network (mobile hotspot)

2. **Check firewall settings:**
   - Windows Firewall might be blocking
   - Corporate firewall might block SQL connections

3. **Try from different location:**
   - Test from home vs office
   - Test from mobile hotspot

## Solution 5: Use Azure Data Studio

If SSMS is having issues, try Azure Data Studio:
1. Download Azure Data Studio
2. Connect using same credentials
3. Run the script there

## Solution 6: Run Script via Application

If direct connection fails, you can:
1. Use the Database Sync page in your application
2. It will create tables automatically when syncing
3. Or create a simple C# script to run the SQL

## Solution 7: Contact Database Provider

If none of the above work:
- Contact MonsterAPI support
- Verify database is active and accessible
- Check if there are any service outages

## Quick Test Script

Run `TEST_CLOUD_CONNECTION.sql` first to verify basic connectivity before running the full setup.

## Alternative: Create Tables One by One

If the full script times out, you can create tables individually:

1. Start with just `tbl_roles`
2. Then `tbl_users`
3. Continue one table at a time
4. This avoids timeout issues

## Recommended Approach

1. **First:** Test connection with `TEST_CLOUD_CONNECTION.sql`
2. **If that works:** Run `CompleteCloudDatabaseSetup.sql` in smaller batches
3. **If connection fails:** Check network/firewall settings
4. **Last resort:** Create tables via application sync feature
