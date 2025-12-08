using System;

namespace MauiApp2.Models
{
    public class Supplier
    {
        public int supplier_id { get; set; }
        public string supplier_name { get; set; } = string.Empty;
        public string? contact_number { get; set; }
        public string? email { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime created_date { get; set; }
        public DateTime? modified_date { get; set; }
    }
}


