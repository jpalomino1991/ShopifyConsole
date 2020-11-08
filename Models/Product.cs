using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Product
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; }
        public string Vendor { get; set; }
        public string ProductType { get; set; }
        public string Handle { get; set; }
        public string Barcode { get; set; }
        public string InventoryItemId { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public string CompareAtPrice { get; set; }
        public string SKU { get; set; }
        public string Size { get; set; }
    }
}
