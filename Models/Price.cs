using System;
using System.Collections.Generic;
using System.Text;

namespace ShopifyConsole.Models
{
    public class Price
    {
        public string CodigoSistema { get; set; }
        public string CodigoPadre { get; set; }
        public decimal PrecioTV { get; set; }
        public string PermitePromocion { get; set; }
        public decimal Promocion { get; set; }
        public string InicioPromocion { get; set; }
        public string FinPromocion { get; set; }
        public string Id { get; set; }
        public string ParentId { get; set; }
    }
}
