using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Option
    {
        public string name { get; set; }
        public int position { get; set; }
        [JsonIgnore]
        public string values { get; set; }
    }
}
