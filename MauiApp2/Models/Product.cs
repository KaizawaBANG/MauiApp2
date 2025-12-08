using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp2.Models
{
    public class Product
    {
        public int product_id { get; set; }
        public int? brand_id { get; set; }
        public int? category_id { get; set; }
        public int? tax_id { get; set; }
        public string product_name { get; set; } = string.Empty;
        public string product_sku { get; set; } = string.Empty;
        public string? model_number { get; set; }
        public decimal? cost_price { get; set; }
        public decimal sell_price { get; set; }
        public int? quantity { get; set; }
        public bool? status { get; set; }
        public DateTime? created_date { get; set; }
        public DateTime? modified_date { get; set; }
    }
}