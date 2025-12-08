# Database Cleanup Script - Keep Essential Data

## Overview
This script safely removes all transaction/test data while preserving essential master data and system configuration.

## What Will Be KEPT (Essential Data)
✅ **Users and Roles** - All user accounts and role definitions  
✅ **Chart of Accounts** - Complete accounting structure  
✅ **Tax Settings** - All tax configurations (VAT, etc.)  
✅ **Products** - All product master data (names, prices, SKUs)  
✅ **Brands** - All brand definitions  
✅ **Categories** - All category definitions  
✅ **Suppliers** - All supplier information  

## What Will Be DELETED (Transaction Data)
❌ **Sales Orders** - All sales transactions  
❌ **Sales Order Items** - All sales line items  
❌ **Purchase Orders** - All purchase transactions  
❌ **Purchase Order Items** - All purchase line items  
❌ **Stock In Records** - All stock receipt records  
❌ **Stock Out Records** - All stock removal records  
❌ **General Ledger Entries** - All accounting transactions  
❌ **Payments** - All payment records  
❌ **Expenses** - All expense records  
❌ **Accounts Payable** - All AP records  
❌ **Stock Adjustments** - All adjustment records  

## Additional Actions
- **Product Quantities** - Reset to 0 (inventory cleared)
- **Identity Seeds** - Reset to 0 (new records start from 1)

## How to Use

### Step 1: Backup Your Database
**IMPORTANT:** Always backup your database before running cleanup scripts!

```sql
-- Create backup (example)
BACKUP DATABASE [YourDatabaseName] 
TO DISK = 'C:\Backup\YourDatabaseName_Backup.bak';
```

### Step 2: Run the Cleanup Script
1. Open SQL Server Management Studio (SSMS)
2. Connect to your database
3. Open `CleanDatabaseKeepEssentialData.sql`
4. Review the script
5. Execute the script (F5)

### Step 3: Verify Results
After running, verify:
- Users still exist
- Products still exist
- Chart of Accounts intact
- All transaction tables are empty
- Product quantities are 0

## Safety Features
- ✅ **Transaction-based** - All changes in one transaction
- ✅ **Rollback on error** - If anything fails, all changes are rolled back
- ✅ **Count reporting** - Shows how many records were deleted
- ✅ **5-second delay** - Gives you time to cancel (Ctrl+C)

## What Happens After Cleanup
1. All transaction history is removed
2. Product inventory is reset to 0
3. Accounting ledger is cleared
4. System is ready for fresh start
5. All master data remains intact

## When to Use This Script
- ✅ Starting fresh with test data
- ✅ Removing all transaction history
- ✅ Resetting inventory to zero
- ✅ Clearing accounting entries
- ✅ Preparing for production deployment

## When NOT to Use This Script
- ❌ If you have real business data you want to keep
- ❌ If you need transaction history
- ❌ If you need accounting records
- ❌ Without a database backup

## Customization
If you want to keep certain data, you can comment out specific sections:

```sql
-- To keep Sales Orders, comment out:
-- DELETE FROM tbl_sales_order_items;
-- DELETE FROM tbl_sales_order;

-- To keep Product Quantities, comment out:
-- UPDATE tbl_product SET quantity = 0;
```

## Troubleshooting
If you get foreign key errors:
- The script deletes in the correct order (child tables first)
- If errors occur, the transaction will rollback automatically
- Check that all tables exist before running

## Next Steps After Cleanup
1. Verify essential data is intact
2. Test creating a new sale
3. Test creating a new purchase order
4. Verify accounting entries are created correctly
5. Check that product quantities update properly




