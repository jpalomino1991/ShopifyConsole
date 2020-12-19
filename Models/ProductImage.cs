using System.ComponentModel.DataAnnotations;

namespace ShopifyConsole.Models
{
    public class ProductImage
    {
        [Key]
        public string id { get; set; }
        public string product_id { get; set; }
        public string alt { get; set; }
        public string src { get; set; }
    }
}