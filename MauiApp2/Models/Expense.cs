using System;

namespace MauiApp2.Models
{
    public class Expense
    {
        public int expense_id { get; set; }
        public DateTime expense_date { get; set; }
        public string category { get; set; } = string.Empty; // Rent, Utilities, Salaries, Supplies, etc.
        public string description { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public string payment_method { get; set; } = string.Empty; // Cash, Card, GCash, PayMaya, Bank Transfer
        public string? reference_number { get; set; } // Receipt number, invoice number, etc.
        public int created_by { get; set; }
        public DateTime created_date { get; set; }
        public DateTime? modified_date { get; set; }
        
        // Display properties
        public string? created_by_name { get; set; }
    }
}




