using System;

namespace MauiApp2.Models
{
    public class Payment
    {
        public int payment_id { get; set; }
        public int? ap_id { get; set; } // Links to accounts payable
        public DateTime payment_date { get; set; }
        public decimal amount { get; set; }
        public string payment_method { get; set; } = string.Empty; // Cash, Card, GCash, PayMaya, Bank Transfer
        public string? reference_number { get; set; } // Check number, transaction number, etc.
        public string? notes { get; set; }
        public int created_by { get; set; }
        public DateTime created_date { get; set; }
        
        // Display properties
        public string? created_by_name { get; set; }
        public string? supplier_name { get; set; }
    }
}




