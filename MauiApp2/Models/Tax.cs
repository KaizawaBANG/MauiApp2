using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp2.Models
{
    public class Tax
    {
        public int tax_id { get; set; }
        public string tax_name { get; set; } = string.Empty;
        public string tax_type { get; set; } = string.Empty;
        public decimal tax_rate { get; set; }
        public bool is_active { get; set; }
        public DateTime created_date { get; set; }
    }
}
