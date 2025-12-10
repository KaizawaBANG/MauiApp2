using System;

namespace MauiApp2.Models
{
    public class SalesOrder
    {
        public int sales_order_id { get; set; }
        public string sales_order_number { get; set; } = string.Empty;
        public DateTime sales_date { get; set; }
        public decimal subtotal { get; set; }
        public decimal tax_amount { get; set; }
        public decimal total_amount { get; set; }
        public string payment_method { get; set; } = string.Empty;
        public int? customer_id { get; set; } // NULL for walk-in customers
        public int processed_by { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? processed_by_name { get; set; }
        public int? item_count { get; set; }
        public string? customer_name { get; set; }
        public string? customer_contact { get; set; }
        public string? customer_email { get; set; }
        public string? customer_address { get; set; }
    }
}







