using System;

namespace MauiApp2.Models
{
    public class SalesOrderItem
    {
        public int sales_order_item_id { get; set; }
        public int sales_order_id { get; set; }
        public int product_id { get; set; }
        public int quantity { get; set; }
        public decimal unit_price { get; set; }
        public decimal tax_rate { get; set; }
        public decimal tax_amount { get; set; }
        public decimal subtotal { get; set; }
        public decimal total { get; set; }
        
        // Display properties
        public string? product_name { get; set; }
        public string? product_sku { get; set; }
    }
}







