using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Variant
    {
        public string id { get; set; }
        public string product_id { get; set; }
        public decimal price { get; set; }
        public string sku { get; set; }
        public int grams { get; set; }
        public string weight { get; set; }
        public string weight_unit { get; set; }
        public string compare_at_price { get; set; }
        public string option1 { get; set; }
        public string inventory_item_id { get; set; }
        public string inventory_management { get; set; }
        public int inventory_quantity { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
