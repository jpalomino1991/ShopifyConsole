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
        public string status { get; set; }
        public string metafields_global_title_tag { get; set; }
        public string metafields_global_description_tag { get; set; }
        public List<Variant> variants { get; set; }
        public List<Option> options { get; set; }
        public List<ImageShopify> images { get; set; }
    }
}