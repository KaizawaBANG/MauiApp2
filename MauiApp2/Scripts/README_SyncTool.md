# Database Sync Tool

This tool syncs data from your LOCAL database to your CLOUD database.

## Setup Instructions

### Option 1: Run as Console App (Recommended)

1. **Update Connection Strings** in `DatabaseSyncTool.cs`:
   - `LOCAL_CONNECTION_STRING` - Your local database connection
   - `CLOUD_CONNECTION_STRING` - Your cloud database connection

2. **Create a new Console App project** (or add to existing):
   ```bash
   dotnet new console -n DatabaseSyncTool
   cd DatabaseSyncTool
   dotnet add package Microsoft.Data.SqlClient
   ```

3. **Copy the `DatabaseSyncTool.cs` file** to your console app project

4. **Run the sync tool**:
   ```bash
   dotnet run
   ```

### Option 2: Add to Existing Project

1. Add a new Console Application project to your solution
2. Add NuGet package: `Microsoft.Data.SqlClient`
3. Copy `DatabaseSyncTool.cs` to the new project
4. Update connection strings
5. Run the project

## What It Does

- Connects to both LOCAL and CLOUD databases
- Copies all data from LOCAL to CLOUD
- Handles IDENTITY columns properly
- Skips tables that don't exist
- Skips rows that already exist (by primary key)
- Shows progress for each table

## Tables Synced (in order):

1. tbl_roles
2. tbl_users
3. tbl_category
4. tbl_brand
5. tbl_tax
6. tbl_product
7. tbl_supplier
8. tbl_purchase_order
9. tbl_purchase_order_items
10. tbl_stock_in
11. tbl_stock_in_items
12. tbl_sales_order
13. tbl_sales_order_items
14. tbl_stock_out
15. tbl_stock_out_items

## Important Notes

- **Make sure all tables exist in CLOUD database first** (run all CREATE TABLE scripts)
- The tool will skip rows that already exist (based on primary key)
- Safe to run multiple times - it won't create duplicates
- Make sure your cloud database connection string has the correct password encoding

## Troubleshooting

- **Connection errors**: Check your connection strings
- **Table not found**: Make sure tables exist in cloud database
- **Identity errors**: The tool handles IDENTITY columns automatically
- **Duplicate key errors**: The tool checks for existing rows before inserting







