using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class ImageShopify
    {
        public string id { get; set; }
        public string product_id { get; set; }
        public string attachment { get; set; }
        public string filename { get; set; }
        public string alt { get; set; }
        public string src { get; set; }
    }
}
