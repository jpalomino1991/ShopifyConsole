using Newtonsoft.Json;
using System.Collections.Generic;

namespace ShopifyConsole.Models
{
    public class ProductShopify
    {
        public string id { get; set; }
        public string title { get; set; }
        public string body_html { get; set; }        
        public string vendor { get; set; }
        public string product_type { get; set; }
        public string handle { get; set; }
        public string tags { get; set; }
        public List<Variant> variants { get; set; }
        public List<Option> options { get; set; }
        public ImageShopify image { get; set; }
    }
}