using System;

namespace MauiApp2.Models
{
    public class PurchaseOrder
    {
        public int po_id { get; set; }
        public int supplier_id { get; set; }
        public string po_number { get; set; } = string.Empty;
        public DateTime order_date { get; set; }
        public DateTime expected_date { get; set; }
        public string status { get; set; } = "Pending";
        public decimal subtotal { get; set; }
        public decimal tax_amount { get; set; }
        public decimal total_amount { get; set; }
        public string? notes { get; set; }
        public string? cancellation_reason { get; set; }
        public string? cancellation_remarks { get; set; }
        public DateTime created_date { get; set; }
        public DateTime? modified_date { get; set; }
        
        // Display properties
        public string? supplier_name { get; set; }
    }
}


