# Fix IDENTITY Seed Issues

## Problem
When adding new records, IDs jump to 1000+ (e.g., 1006) instead of continuing sequentially from the last record.

## Root Cause
The SQL Server IDENTITY seed has been set to a high value (e.g., 1005), causing new records to start from 1006 instead of continuing from the maximum existing ID.

## Solution

### Option 1: Quick Fix (Products Only)
Run `FixProductIdentitySeed.sql` to fix just the product table:
```sql
-- This will reset the product identity seed to continue from the max product_id
```

### Option 2: Fix All Tables
Run `FixIdentitySeedsForAllTables.sql` to fix all tables at once:
- tbl_roles
- tbl_users
- tbl_category
- tbl_brand
- tbl_tax
- tbl_product
- tbl_supplier
- tbl_purchase_order
- tbl_purchase_order_items
- tbl_stock_in
- tbl_stock_in_items
- tbl_sales_order
- tbl_sales_order_items
- tbl_stock_out
- tbl_stock_out_items

### Option 3: Delete High ID Records First
If you have test records with IDs >= 1000 that you want to remove:
1. Run `FixHighIdRecords.sql` to delete all records with ID >= 1000
2. Then run `FixIdentitySeedsForAllTables.sql` to reset the seeds

## How to Use

1. **Open SQL Server Management Studio (SSMS)** or your preferred SQL client
2. **Connect to your database**
3. **Open the script file** you want to run
4. **Execute the script**
5. **Check the output messages** to see what was fixed

## Example Output
```
=== Fixing IDENTITY Seeds for All Tables ===
✓ Fixed tbl_product IDENTITY seed. Current max ID: 8
  Next product ID will be: 9
```

## Important Notes

- **These scripts do NOT delete any records** (except FixHighIdRecords.sql)
- They only reset the IDENTITY seed to continue from the maximum existing ID
- If you have records with IDs >= 1000, the next ID will be 1001 (or max + 1)
- To get sequential IDs starting from 9, you need to delete high ID records first

## Prevention

To prevent this issue in the future:
- Avoid manually inserting records with explicit IDs using `IDENTITY_INSERT`
- If you must use `IDENTITY_INSERT`, always reset the seed afterward
- Run `FixIdentitySeedsForAllTables.sql` periodically if you notice ID gaps

## Tables That May Have This Issue

Based on the codebase, these tables use IDENTITY columns and may need fixing:
- ✅ **tbl_product** (confirmed issue - IDs jumping to 1006)
- ⚠️ **tbl_category** (may have same issue)
- ⚠️ **tbl_brand** (may have same issue)
- ⚠️ **tbl_tax** (may have same issue)
- ⚠️ **tbl_supplier** (may have same issue)
- ⚠️ **tbl_users** (may have same issue)
- ⚠️ All order and stock tables

**Recommendation**: Run `FixIdentitySeedsForAllTables.sql` to fix all tables at once.





