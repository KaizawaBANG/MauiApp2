using System;

namespace MauiApp2.Models
{
    public class AccountsPayable
    {
        public int ap_id { get; set; }
        public int? po_id { get; set; }
        public int supplier_id { get; set; }
        public string? invoice_number { get; set; }
        public decimal total_amount { get; set; }
        public decimal paid_amount { get; set; }
        public decimal balance_amount { get; set; } // Calculated: total_amount - paid_amount
        public DateTime? due_date { get; set; }
        public string status { get; set; } = "Unpaid"; // Unpaid, Partial, Paid
        public DateTime created_date { get; set; }
        public DateTime? modified_date { get; set; }
        
        // Display properties
        public string? supplier_name { get; set; }
        public string? po_number { get; set; }
    }
}




