using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class MasterProduct
    {
        public List<ProductShopify> products { get; set; }
        public ProductShopify product { get; set; }
    }
}
