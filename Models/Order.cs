using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopifyConsole.Models
{
    public class Order
    {
		public string id { get; set; }
		public string email { get; set; }
		public DateTime created_at { get; set; }
		public DateTime updated_at { get; set; }
		public int number { get; set; }
		public string token { get; set; }
		public string gateway { get; set; }
		public decimal total_price { get; set; }
		public decimal subtotal_price { get; set; }
		public decimal total_tax { get; set; }
		public string currency { get; set; }
		public string financial_status { get; set; }
		public decimal total_discounts { get; set; }
		public decimal total_line_items_price { get; set; }
		public string name { get; set; }
		public string order_number { get; set; }
		public string customer_id { get; set; }
		public string fulfillment_status { get; set; }
		public string status { get; set; }
		public string fechaEstimada { get; set; }
		public string fulfillment_id { get; set; }
		public decimal shipping_price { get; set; }
		[NotMapped]
		public string cancel_reason { get; set; }
		[NotMapped]
		public ICollection<Item> line_items { get; set; }
		[NotMapped]
		public BillAddress billing_address { get; set; }
		[NotMapped]
		public ShipAddress shipping_address { get; set; }
		[NotMapped]
		public List<ShippingLine> shipping_lines { get; set; }
		[NotMapped]
		public List<Fulfillment> fulfillments { get; set; }
		[NotMapped]
		//public Payment payment_details { get; set; }
		public Customer customer { get; set; }
	}
}
