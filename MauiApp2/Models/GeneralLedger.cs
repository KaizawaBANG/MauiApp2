using System;

namespace MauiApp2.Models
{
    public class GeneralLedger
    {
        public int ledger_id { get; set; }
        public DateTime transaction_date { get; set; }
        public int account_id { get; set; }
        public decimal debit_amount { get; set; }
        public decimal credit_amount { get; set; }
        public string description { get; set; } = string.Empty;
        public string? reference_type { get; set; } // Sales, Purchase, Payment, Expense, StockIn, StockOut, Manual
        public int? reference_id { get; set; } // Links to sales_order_id, po_id, expense_id, etc.
        public int created_by { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? account_name { get; set; }
        public string? account_code { get; set; }
        public string? account_type { get; set; } // Asset, Liability, Revenue, Expense, Equity
        public string? created_by_name { get; set; }
    }
}

