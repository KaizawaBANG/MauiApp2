using System;

namespace MauiApp2.Models
{
    public class Customer
    {
        public int customer_id { get; set; }
        public string customer_name { get; set; } = string.Empty;
        public string? contact_number { get; set; }
        public string? email { get; set; }
        public string? address { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime created_date { get; set; }
        public DateTime? modified_date { get; set; }
    }
}

