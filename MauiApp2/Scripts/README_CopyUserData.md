# Copy User Data to New Database

This guide explains how to copy user data (including password hashes) from your old database to a new clean database.

## Why Copy User Data?

When creating a new clean database, you typically want to:
- Start fresh with empty tables
- But keep your existing users and their login credentials
- Preserve password hashes so users don't need to reset passwords

## Prerequisites

1. **Both databases must exist:**
   - Your old database (with user data)
   - Your new clean database (with table structure created)

2. **Tables must exist in new database:**
   - `tbl_roles` table
   - `tbl_users` table

3. **You have access to both databases**

## Method 1: Using the Simple Script (Recommended)

1. **Open SQL Server Management Studio (SSMS)**

2. **Connect to your NEW database**

3. **Open the script:** `CopyUserDataToNewDatabase_Simple.sql`

4. **Update the script:**
   - Replace `YourNewDatabaseName` with your actual new database name (line 7)
   - Replace `YourOldDatabaseName` with your actual old database name (lines 20 and 35)

5. **Run the script**

6. **Verify:**
   - Check that users were copied
   - Try logging in with existing credentials

## Method 2: Using the Advanced Script

The `CopyUserDataToNewDatabase.sql` script uses variables and is more flexible:

1. Open the script
2. Update the `@SourceDatabase` variable (your old database name)
3. Update the `@TargetDatabase` variable (your new database name)
4. Run while connected to any database

## What Gets Copied?

✅ **User ID** - Preserves original user IDs  
✅ **Username** - All usernames  
✅ **Password Hash** - All password hashes (users can log in with same passwords)  
✅ **Full Name** - User names  
✅ **Email** - Email addresses  
✅ **Role ID** - User roles  
✅ **Is Active** - Active status  
✅ **Last Login** - Last login timestamp  
✅ **Created Date** - Account creation date  

## Important Notes

1. **Password Hashes are Safe:**
   - Password hashes are already encrypted (SHA256)
   - They cannot be reversed to get original passwords
   - Copying them is safe and standard practice

2. **Roles Must Match:**
   - The script copies roles first
   - Role IDs must match between databases
   - If role IDs don't match, you'll need to update them manually

3. **Identity Seed Reset:**
   - The script resets the identity seed after copying
   - This prevents ID conflicts when creating new users

4. **Duplicate Prevention:**
   - The script checks for existing users before copying
   - Won't create duplicates if you run it multiple times

## Troubleshooting

### Error: "Invalid object name"
- **Solution:** Make sure both databases exist and tables are created in the new database

### Error: "Foreign key constraint"
- **Solution:** Make sure roles are copied first, or role IDs match

### Users can't log in after copy
- **Check:** Verify password hashes were copied correctly
- **Check:** Make sure role_id values match between databases

### Identity seed issues
- **Solution:** The script automatically resets the identity seed
- If you still have issues, run: `DBCC CHECKIDENT ('tbl_users', RESEED, [max_user_id])`

## After Copying

1. **Test login** with existing user credentials
2. **Verify user data** in the new database
3. **Check roles** are assigned correctly
4. **Continue with your new clean database** - all other tables will be empty and ready for fresh data

## Example

```sql
-- Old database: MyAppDB_Old
-- New database: MyAppDB_New

-- In CopyUserDataToNewDatabase_Simple.sql, change:
USE [MyAppDB_New];  -- Your new database

-- And change:
FROM [MyAppDB_Old].[dbo].[tbl_users]  -- Your old database
```

Then run the script while connected to `MyAppDB_New`.





