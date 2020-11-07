using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Item
    {
        public string id { get; set; }
        public string variant_id { get; set; }
        public string title { get; set; }
        public int quantity { get; set; }
        public string sku { get; set; }
        public string product_id { get; set; }
        public string tax_code { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }
        public string pre_tax_price { get; set; }
        public string order_id { get; set; }
        public decimal tax_price { get; set; }
        public decimal tax_rate { get; set; }
        [NotMapped]
        public ICollection<Tax> tax_lines { get; set; }
    }
}
