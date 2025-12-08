using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MauiApp2.Models
{
    public class Brand
    {
        public int brand_id { get; set; }
        public string brand_name { get; set; } = string.Empty;
        public string brand_code { get; set; } = string.Empty; // 2-3 character code for SKU generation
        public string? description { get; set; }
    }
}
