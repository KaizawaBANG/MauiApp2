# SKU Generation Update - CATEGORY-BRAND-NUMBER Format

## Overview
Updated SKU generation from `BRAND-CATEGORY-NUMBER` to the recommended format: `CATEGORY-BRAND-NUMBER` using short codes.

## What Changed

### 1. Database Schema
- ✅ Added `category_code` column to `tbl_category` (NVARCHAR(10), unique)
- ✅ Added `brand_code` column to `tbl_brand` (NVARCHAR(10), unique)
- ✅ Migration script: `AddCategoryAndBrandCodes.sql`

### 2. Models
- ✅ `Category.cs` - Added `category_code` property
- ✅ `Brand.cs` - Added `brand_code` property

### 3. Services
- ✅ `ProductService.cs` - Updated SKU generation logic:
  - Format changed: `CATEGORY-BRAND-NUMBER` (was: `BRAND-CATEGORY-NUMBER`)
  - Uses codes instead of full names
  - Sequential numbering per category-brand combo
  - Example: `REF-SAM-0001` (Refrigerator, Samsung)

- ✅ `CategoryService.cs` - Updated to handle `category_code`:
  - Auto-generates code if not provided (first 4 chars)
  - Includes code in all CRUD operations

- ✅ `BrandService.cs` - Updated to handle `brand_code`:
  - Auto-generates code if not provided (first 3 chars)
  - Includes code in all CRUD operations

### 4. Table Creation Script
- ✅ `CreateAllTablesForCloud_db34089.sql` - Updated to include code columns

## New SKU Format

### Format: `CATEGORY-BRAND-NUMBER`

**Examples:**
- `REF-SAM-0001` - Refrigerator, Samsung, #1
- `WASH-LG-0001` - Washing Machine, LG, #1
- `AC-GEN-0001` - Air Conditioner, Generic, #1
- `MISC-GEN-0001` - Miscellaneous, Generic, #1

### Code Generation Rules

**Category Code:**
- 3-4 characters
- Generated from first 4 alphanumeric characters of category name
- Uppercase, no spaces/special chars
- Default: "MISC" if no category

**Brand Code:**
- 2-3 characters
- Generated from first 3 alphanumeric characters of brand name
- Uppercase, no spaces/special chars
- Default: "GEN" if no brand

**Number:**
- 4 digits (0001, 0002, etc.)
- Sequential per category-brand combination
- Auto-increments for each new product in same category-brand

## Implementation Steps

### Step 1: Run Migration Script
```sql
-- Run this first to add code columns to existing tables
EXEC AddCategoryAndBrandCodes.sql
```

This will:
- Add `category_code` and `brand_code` columns
- Generate codes for existing categories and brands
- Create unique indexes

### Step 2: Review Generated Codes
Check the generated codes and adjust if needed:
```sql
SELECT category_id, category_name, category_code FROM tbl_category;
SELECT brand_id, brand_name, brand_code FROM tbl_brand;
```

### Step 3: Test SKU Generation
Create a new product and verify SKU format:
- Should be: `CATEGORY-BRAND-NUMBER`
- Should be short (12-13 characters)
- Should be unique

## Benefits

✅ **Shorter SKUs** - 12-13 chars vs 25-30 chars  
✅ **More Scalable** - Works with thousands of products  
✅ **Professional** - Industry-standard format  
✅ **Meaningful** - Still shows category and brand  
✅ **Sequential** - Proper numbering per category-brand combo  

## Backward Compatibility

⚠️ **Note:** Existing products will keep their old SKU format. Only new products will use the new format.

If you want to update existing products:
1. You'll need a separate migration script
2. Or manually update SKUs for existing products

## Next Steps (Optional)

1. **Update UI Pages** - Add code fields to Category/Brand management pages
2. **Update Existing Products** - Migrate old SKUs to new format (if needed)
3. **Validation** - Add validation to ensure codes are unique and valid

## Testing

After implementation, test:
1. Create new category → Verify code is generated
2. Create new brand → Verify code is generated
3. Create new product → Verify SKU format: `CATEGORY-BRAND-NUMBER`
4. Create multiple products same category-brand → Verify sequential numbering




