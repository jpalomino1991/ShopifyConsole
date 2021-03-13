using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class CustomerAddress
    {
        public string id { get; set; }        
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string province { get; set; }
        public string country { get; set; }
        public string company { get; set; }
        public string zip { get; set; }
        public string name { get; set; }
        public string province_code { get; set; }
        public string country_code { get; set; }
        public string customer_id { get; set; }
        public string phone { get; set; }
    }
}
