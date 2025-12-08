using System;
using System.Collections.Generic;

namespace MauiApp2.Models
{
    public class StockIn
    {
        public int stock_in_id { get; set; }
        public int po_id { get; set; }
        public int? supplier_id { get; set; }
        public string stock_in_number { get; set; } = string.Empty;
        public DateTime stock_in_date { get; set; }
        public string? notes { get; set; }
        public int processed_by { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? supplier_name { get; set; }
        public string? po_number { get; set; }
        public string? processed_by_name { get; set; }
    }
}







