using System;

namespace MauiApp2.Models
{
    public class PurchaseOrderItem
    {
        public int po_items_id { get; set; }
        public int po_id { get; set; }
        public int product_id { get; set; }
        public int quantity_ordered { get; set; }
        public decimal unit_cost { get; set; }
        public decimal tax_rate { get; set; }
        public decimal tax_amount { get; set; }
        public decimal subtotal { get; set; }
        public decimal total { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? product_name { get; set; }
        public string? product_sku { get; set; }
    }
}


