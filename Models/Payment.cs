using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopifyConsole.Models
{
    public class Payment
    {
        [Key]
        public string id { get; set; }
        public string order_id { get; set; }
        public string kind { get; set; }
        public string gateway { get; set; }
        public string status { get; set; }
        public string message { get; set; }
        public string authorization { get; set; }
        public DateTime processed_at { get; set; }
        [NotMapped]
        public PaymentDetail receipt { get; set; }
        public string x_signature { get; set; }
        public string x_reference { get; set; }
        public string x_account_id { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
    }
}
