using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Customer
    {
        public string id { get; set; }
        public string email { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string phone { get; set; }
        public string currency { get; set; }
        [NotMapped]
        public CustomerAddress default_address { get; set; }
    }
}
