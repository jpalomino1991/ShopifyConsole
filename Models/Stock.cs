using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Stock
    {
        public string CodigoSistema { get; set; }
        public string CodigoPadre { get; set; }
        public int StockTotal { get; set; }
        public string InventoryItemId { get; set; }
    }
}
