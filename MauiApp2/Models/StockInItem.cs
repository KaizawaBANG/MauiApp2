using System;

namespace MauiApp2.Models
{
    public class StockInItem
    {
        public int stock_in_items_id { get; set; }
        public int stock_in_id { get; set; }
        public int product_id { get; set; }
        public int quantity_received { get; set; }
        public int quantity_rejected { get; set; }
        public string? rejection_reason { get; set; }
        public string? rejection_remarks { get; set; }
        public decimal unit_cost { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? product_name { get; set; }
        public string? product_sku { get; set; }
        public int? quantity_ordered { get; set; } // From PO if applicable
    }
}

