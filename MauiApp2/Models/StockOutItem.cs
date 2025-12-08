using System;

namespace MauiApp2.Models
{
    public class StockOutItem
    {
        public int stock_out_items_id { get; set; }
        public int stock_out_id { get; set; }
        public int product_id { get; set; }
        public int quantity { get; set; }
        public string reason { get; set; } = "Sale";
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? product_name { get; set; }
        public string? product_sku { get; set; }
        public decimal? cost_price { get; set; }
    }
}





