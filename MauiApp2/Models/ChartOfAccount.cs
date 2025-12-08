using System;

namespace MauiApp2.Models
{
    public class ChartOfAccount
    {
        public int account_id { get; set; }
        public string account_code { get; set; } = string.Empty;
        public string account_name { get; set; } = string.Empty;
        public string account_type { get; set; } = string.Empty; // Asset, Liability, Equity, Revenue, Expense
        public string? description { get; set; }
        public bool is_active { get; set; } = true;
        public DateTime created_date { get; set; }
    }
}




