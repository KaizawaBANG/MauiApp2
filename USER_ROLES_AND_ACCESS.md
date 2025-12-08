# User Roles and Access Control

## Recommended User Roles

Based on your inventory management system, here are the recommended user roles and what each role should be able to access:

---

## 1. **Administrator** (Full Access)
**Purpose**: System administrator with complete access to all features.

**Access:**
- ✅ Dashboard
- ✅ User Management
  - User List (View, Add, Edit, Delete users)
  - Roles & Permissions (if implemented)
- ✅ Reports
  - Sales Summary
  - Inventory Report
  - Purchase Order Report
- ✅ Inventory Management
  - Product List
  - Category
  - Brand
  - Tax
  - Purchase Order
  - Supplier
  - Stock In
  - Sale Transaction
  - Inventory Transactions
- ✅ Accounting
  - Chart of Accounts
  - Accounts Payable
  - Expenses
  - General Ledger
- ✅ Database Sync

**Typical Users:**
- IT Administrator
- System Owner
- Company Owner

---

## 2. **Inventory Manager**
**Purpose**: Manages all inventory operations, purchasing, and stock movements.

**Access:**
- ✅ Dashboard
- ✅ User Management (View only - cannot add/edit users)
- ✅ Reports
  - Sales Summary
  - Inventory Report
  - Purchase Order Report
- ✅ Inventory Management
  - Product List (Full access)
  - Category (Full access)
  - Brand (Full access)
  - Tax (Full access)
  - Purchase Order (Create, View, Edit, Approve)
  - Supplier (Full access)
  - Stock In (Full access)
  - Sale Transaction (View only)
  - Inventory Transactions (Full access)
- ❌ Accounting (No access)
- ✅ Database Sync

**Typical Users:**
- Warehouse Manager
- Inventory Supervisor
- Purchasing Manager

---

## 3. **Accountant**
**Purpose**: Handles all financial and accounting operations.

**Access:**
- ✅ Dashboard
- ✅ User Management (View only)
- ❌ Reports (No access to inventory reports)
- ❌ Inventory Management (No access)
- ✅ Accounting
  - Chart of Accounts (Full access)
  - Accounts Payable (Full access)
  - Expenses (Full access)
  - General Ledger (Full access)
- ❌ Database Sync

**Typical Users:**
- Accountant
- Bookkeeper
- Financial Analyst

---

## 4. **Finance Manager**
**Purpose**: Oversees financial operations and accounting.

**Access:**
- ✅ Dashboard
- ✅ User Management (View only)
- ❌ Reports (No access to inventory reports)
- ❌ Inventory Management (No access)
- ✅ Accounting
  - Chart of Accounts (Full access)
  - Accounts Payable (Full access)
  - Expenses (Full access)
  - General Ledger (Full access)
- ❌ Database Sync

**Typical Users:**
- Finance Manager
- CFO
- Financial Controller

---

## 5. **Sales Clerk / Cashier** (Recommended Addition)
**Purpose**: Handles sales transactions at point of sale.

**Access:**
- ✅ Dashboard (Limited view - sales metrics only)
- ❌ User Management (No access)
- ❌ Reports (No access)
- ❌ Inventory Management (Limited access)
  - Product List (View only - for product lookup)
  - Sale Transaction (Create sales, View own sales)
  - Inventory Transactions (View only - sales transactions)
- ❌ Accounting (No access)
- ❌ Database Sync

**Typical Users:**
- Sales Staff
- Cashier
- Store Clerk

---

## 6. **Warehouse Staff** (Recommended Addition)
**Purpose**: Handles stock receiving and inventory operations.

**Access:**
- ✅ Dashboard (Limited view)
- ❌ User Management (No access)
- ❌ Reports (No access)
- ❌ Inventory Management (Limited access)
  - Stock In (Mark as delivered, Receive stock)
  - Purchase Order (View only - to see what to receive)
  - Inventory Transactions (View only)
- ❌ Accounting (No access)
- ❌ Database Sync

**Typical Users:**
- Warehouse Worker
- Stock Receiver
- Inventory Clerk

---

## 7. **Sales Manager** (Recommended Addition)
**Purpose**: Manages sales operations and views sales reports.

**Access:**
- ✅ Dashboard
- ❌ User Management (No access)
- ✅ Reports
  - Sales Summary (Full access)
  - Inventory Report (View only)
- ❌ Inventory Management (Limited access)
  - Product List (View only)
  - Sale Transaction (Full access)
  - Inventory Transactions (View only - sales related)
- ❌ Accounting (No access)
- ❌ Database Sync

**Typical Users:**
- Sales Manager
- Sales Supervisor

---

## Current Implementation Status

### ✅ Currently Implemented:
- **Administrator** - Full access (as defined above)
- **Inventory Manager** - Inventory and Reports access
- **Accountant** - Accounting access
- **Finance Manager** - Accounting access

### ⚠️ Needs Implementation:
- **Sales Clerk / Cashier** - Currently no role restriction on Sales Transaction
- **Warehouse Staff** - Currently no role restriction on Stock In
- **Sales Manager** - Currently no role restriction on Reports

---

## Access Control Matrix

| Feature | Administrator | Inventory Manager | Accountant | Finance Manager | Sales Clerk* | Warehouse Staff* | Sales Manager* |
|---------|--------------|-------------------|------------|-----------------|--------------|------------------|----------------|
| Dashboard | ✅ | ✅ | ✅ | ✅ | ✅ (Limited) | ✅ (Limited) | ✅ |
| User Management | ✅ Full | ✅ View | ✅ View | ✅ View | ❌ | ❌ | ❌ |
| Reports - Sales | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Reports - Inventory | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ (View) |
| Reports - PO | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Product List | ✅ Full | ✅ Full | ❌ | ❌ | ✅ View | ❌ | ✅ View |
| Category/Brand/Tax | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ❌ | ❌ |
| Purchase Order | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ✅ View | ❌ |
| Supplier | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ❌ | ❌ |
| Stock In | ✅ Full | ✅ Full | ❌ | ❌ | ❌ | ✅ Receive | ❌ |
| Sale Transaction | ✅ Full | ✅ View | ❌ | ❌ | ✅ Create | ❌ | ✅ Full |
| Inventory Transactions | ✅ Full | ✅ Full | ❌ | ❌ | ✅ View (Sales) | ✅ View | ✅ View (Sales) |
| Accounting | ✅ Full | ❌ | ✅ Full | ✅ Full | ❌ | ❌ | ❌ |
| Database Sync | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

*These roles need to be implemented in the code.

---

## Recommendations

1. **Add Role-Based Restrictions to Sales Transaction**
   - Currently, anyone with inventory access can create sales
   - Should restrict to: Administrator, Inventory Manager, Sales Clerk, Sales Manager

2. **Add Role-Based Restrictions to Stock In**
   - Currently, anyone with inventory access can receive stock
   - Should restrict receiving to: Administrator, Inventory Manager, Warehouse Staff

3. **Implement View-Only Access**
   - Some roles should have "View Only" access (cannot edit/delete)
   - Currently, access is binary (all or nothing)

4. **Add Sales Clerk Role**
   - Essential for point-of-sale operations
   - Should only access Sales Transaction and view products

5. **Add Warehouse Staff Role**
   - Essential for receiving stock
   - Should only access Stock In and view Purchase Orders

6. **Restrict User Management**
   - Only Administrator should be able to add/edit/delete users
   - Other roles should only view user list

---

## Next Steps

1. Create additional roles in the database (`tbl_roles` table)
2. Update `Sidebar.razor` to add role checks for new roles
3. Add page-level authorization to restrict access based on roles
4. Implement "View Only" vs "Full Access" permissions
5. Test each role to ensure proper access control



