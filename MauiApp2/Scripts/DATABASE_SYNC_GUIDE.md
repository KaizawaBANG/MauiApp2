# Database Sync Guide 📊

## Overview
Your application now supports syncing data from your **local database** to your **MonsterAPI cloud database**. The sync feature is accessible through the Database Sync page.

## Setup

### 1. Connection Strings Configuration
Both connection strings are stored in `App.config`:

```xml
<connectionStrings>
    <!-- Local Database (your development database) -->
    <add name="DefaultConnection"
         connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DB_QuadTech;Integrated Security=True" />
    
    <!-- Cloud Database (MonsterAPI) -->
    <add name="CloudConnection"
         connectionString="Data Source=db33496.public.databaseasp.net,1433;Initial Catalog=db33496;User ID=db33496;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;" />
</connectionStrings>
```

### 2. Access the Sync Page
Navigate to: **Database Sync** (accessible from the sidebar menu)

Or go directly to: `/database-sync`

## How to Use

### Step 1: Test Connections
1. Click the **"Test Connections"** button
2. Verify both databases are accessible:
   - ✅ Local Database: Connected
   - ✅ Cloud Database: Connected

### Step 2: Sync Data
1. Click the **"Sync to Cloud"** button
2. Wait for the sync to complete (progress bar will show status)
3. Review the sync results:
   - Tables processed
   - Rows copied
   - Any errors or warnings

## What Gets Synced

The sync process copies data from local to cloud for these tables (in order):
1. `tbl_roles`
2. `tbl_users`
3. `tbl_category`
4. `tbl_brand`
5. `tbl_tax`
6. `tbl_product`
7. `tbl_supplier`
8. `tbl_purchase_order`
9. `tbl_purchase_order_items`
10. `tbl_stock_in`
11. `tbl_stock_in_items`
12. `tbl_sales_order`
13. `tbl_sales_order_items`
14. `tbl_stock_out`
15. `tbl_stock_out_items`

## Important Features

### ✅ Safe to Run Multiple Times
- The sync process **skips records that already exist** (based on primary key)
- You can run sync multiple times without creating duplicates
- Only new records will be added

### ✅ Identity Seed Protection
- **Automatically resets identity seeds** after sync
- Prevents ID jumps (1000+) in the cloud database
- Ensures new records continue sequentially

### ✅ Foreign Key Relationships
- Tables are synced in the correct order
- Respects foreign key dependencies
- Prevents constraint violations

## Workflow

### Initial Setup (First Time)
1. **Create tables in cloud database** (if not already created)
   - Run your table creation scripts on the cloud database
   - Or let the sync process handle it (if tables don't exist, sync will skip them)

2. **Test connections** to ensure both databases are accessible

3. **Run sync** to copy all local data to cloud

### Regular Use
- **Add/Edit data locally** in your application
- **When ready**, click "Sync to Cloud" to update the cloud database
- The sync will only copy new/changed records

## Troubleshooting

### Connection Failed
- Check your connection strings in `App.config`
- Verify network connectivity to MonsterAPI
- Check firewall settings
- Ensure SQL Server allows remote connections

### Sync Errors
- Check that all tables exist in the cloud database
- Verify table structures match between local and cloud
- Review the sync log for specific error messages

### Identity Seed Issues
- The sync automatically resets identity seeds
- If you still see ID jumps, run `FixIdentitySeedsForAllTables.sql` on the cloud database

## Best Practices

1. **Test connections first** before syncing
2. **Backup your cloud database** before major syncs (if possible)
3. **Sync regularly** to keep cloud database up-to-date
4. **Monitor sync logs** for any warnings or errors
5. **Use local database for development**, sync to cloud for production/backup

## Notes

- **Local Database**: Your primary development database with all your data
- **Cloud Database**: MonsterAPI database used for backup, production, or remote access
- Sync is **one-way**: Local → Cloud (not bidirectional)
- The sync process may take several minutes depending on data size

## Security

⚠️ **Important**: 
- Connection strings contain sensitive information
- Keep `App.config` secure and don't commit passwords to public repositories
- Consider using environment variables or secure configuration for production





