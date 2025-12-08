using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp2.Models
{
    public class User
    {
        public int user_id { get; set; }
        public int role_id { get; set; }
        public string username { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password_hash { get; set; } = string.Empty;
        public string full_name { get; set; } = string.Empty;
        public bool is_active { get; set; } = true;
        public DateTime? last_login { get; set; }
        public DateTime created_date { get; set; } = DateTime.Now;
    }
}