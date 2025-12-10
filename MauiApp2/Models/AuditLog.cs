using System;

namespace MauiApp2.Models
{
    public class AuditLog
    {
        public int log_id { get; set; }
        public int user_id { get; set; }
        public string action_type { get; set; } = string.Empty; // Create, Update, Delete, View, Login, Logout
        public string? description { get; set; } // Descriptive message like "added new product: Aircon"
        public string? table_name { get; set; }
        public int? record_id { get; set; }
        public string? old_values { get; set; }
        public string? new_values { get; set; }
        public string? ip_address { get; set; }
        public string? user_agent { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? user_name { get; set; }
        public string? username { get; set; }
    }
}

