using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class ShippingTimes
    {
        public int id { get; set; }
        public int beginDay { get; set; }
        public int endDay { get; set; }
        public string city { get; set; }
    }
}
