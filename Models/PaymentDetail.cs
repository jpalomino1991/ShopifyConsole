using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class PaymentDetail
    {
        public string x_signature { get; set; }
        public string x_reference { get; set; }
        public string x_account_id { get; set; }
    }
}
