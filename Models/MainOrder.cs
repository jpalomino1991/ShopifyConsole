using System.Collections.Generic;

namespace ShopifyConsole.Models
{
    public class MainOrder
    {
        public List<Order> orders { get; set; }
        public Order order { get; set; }
    }
}
