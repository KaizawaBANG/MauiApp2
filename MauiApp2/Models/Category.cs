using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

    namespace MauiApp2.Models
    {
    public class Category
    {
        public int category_id { get; set; }
        public string category_name { get; set; } = string.Empty;
        public string category_code { get; set; } = string.Empty; // 3-4 character code for SKU generation
        public string? description { get; set; }
    }
}

