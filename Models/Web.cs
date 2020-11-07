using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Web
    {
        [Key]
        public int Id { get; set; }
        public string WebURL { get; set; }
        public string WebAPI { get; set; }
        public string WebPassword { get; set; }
        public string LocationId { get; set; }
    }
}
