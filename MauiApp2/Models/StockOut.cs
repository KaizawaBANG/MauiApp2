using System;

namespace MauiApp2.Models
{
    public class StockOut
    {
        public int stock_out_id { get; set; }
        public int? sales_order_id { get; set; }
        public string stock_out_number { get; set; } = string.Empty;
        public DateTime stock_out_date { get; set; }
        public string reason { get; set; } = "Sale";
        public int processed_by { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? processed_by_name { get; set; }
        public string? sales_order_number { get; set; }
    }
}







