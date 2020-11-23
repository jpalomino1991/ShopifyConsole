using System.ComponentModel.DataAnnotations;

namespace ShopifyConsole.Models
{
    public class ProductImage
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
    }
}
