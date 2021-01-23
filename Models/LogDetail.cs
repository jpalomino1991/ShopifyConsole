using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class LogDetail
    {
        public int Id { get; set; }
        public string Error { get; set; }
        public Guid LogId { get; set; }
    }
}
