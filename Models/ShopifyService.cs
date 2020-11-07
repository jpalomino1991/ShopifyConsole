using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ShopifyConsole.Models
{
    public class ShopifyService
    {
        private string webURL;
        private string webAPI;
        private string webPassword;
        //public string connStr;
        private string kellyConnStr;
        private string locationId;
        private readonly Logger logger;
        public ShopifyService(string webURL,string webAPI,string webPassword,string kellyConnStr,string locationId,Logger logger)
        {
            this.webURL = webURL;
            this.webAPI = webAPI;
            this.webPassword = webPassword;
            this.kellyConnStr = kellyConnStr;
            this.locationId = locationId;
            this.logger = logger;
        }
        public void GetProducts()
        {
            try
            {
                IRestResponse response = CallShopify("products.json", Method.GET, null);

                if (response.StatusCode.ToString().Equals("OK"))
                {
                    MasterProduct mp = JsonConvert.DeserializeObject<MasterProduct>(response.Content);
                    if (mp != null)
                    {
                        using (var context = new Models.AppContext(kellyConnStr))
                        {
                            logger.Info("Deleting table Product");
                            context.Product.RemoveRange(context.Product);
                            context.SaveChanges();
                            logger.Info("Delete Successfully");
                            //context.Database.ExecuteSqlCommand("TRUNCATE TABLE dbo.Product");
                            foreach (ProductShopify product in mp.products)
                            {
                                logger.Info("Adding Product " + product.title);
                                Product p = new Product();
                                p.Id = product.id;
                                p.Title = product.title;
                                p.Description = product.body_html;
                                p.Vendor = product.vendor;
                                p.ProductType = product.product_type;
                                p.Tags = product.tags;
                                p.SKU = product.variants[0].sku.Substring(0,product.variants[0].sku.LastIndexOf("."));

                                context.Add(p);

                                foreach (Variant variant in product.variants)
                                {
                                    Product child = new Product();
                                    child.Id = variant.id;
                                    child.ParentId = variant.product_id;
                                    child.SKU = variant.sku;
                                    child.Price = variant.price;
                                    child.CompareAtPrice = variant.compare_at_price;
                                    child.Stock = variant.inventory_quantity;
                                    child.InventoryItemId = variant.inventory_item_id;
                                    child.Size = variant.option1;

                                    context.Add(child);
                                }
                                logger.Info("Product added");
                            }
                            context.SaveChanges();
                        }
                    }
                }
                else
                {
                    logger.Error("Error getting products: " + response.ErrorMessage);
                }
            }
            catch(Exception e)
            {
                logger.Error(e, "Error getting products");
                return;
            }            
        }

        public void UpdateStock()
        {
            try
            {
                List<Stock> lsStock = new List<Stock>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    lsStock = context.Stock.FromSqlInterpolated($"GetStockForShopify {DateTime.Now.ToString("yyyy/MM/dd")}").ToList();
                }                

                if (lsStock.Count > 0)
                {
                    foreach (Stock stock in lsStock)
                    {
                        logger.Info("Updating product stock : " + stock.CodigoSistema);
                        dynamic JProduct = new
                        {
                            location_id = locationId,
                            inventory_item_id = stock.InventoryItemId,
                            available = stock.StockTotal
                        };
                        IRestResponse response = CallShopify("inventory_levels/set.json", Method.POST, JProduct);
                        if (response.StatusCode.ToString().Equals("OK"))
                            logger.Info("Product stock updated");
                        else
                            logger.Error("Error updating stock: " + response.ErrorMessage);
                    }
                }
            }
            catch(Exception e)
            {
                logger.Error(e, "Error updating stock");
                return;
            }
        }

        public void UpdatePrice()
        {
            try
            {
                List<Price> lsPrice = new List<Price>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    lsPrice = context.Price.FromSqlInterpolated($"GetPriceForShopify {DateTime.Now.AddDays(-10).ToString("yyyy/MM/dd")}").ToList();
                }

                if (lsPrice.Count > 0)
                {
                    foreach (Price product in lsPrice)
                    {
                        logger.Info("Updating product price : " + product.CodigoSistema);
                        Console.WriteLine("Updating product price : " + product.CodigoSistema);
                        string compare_price = "";
                        DateTime fechaFin = product.FinPromocion.AddDays(1).AddTicks(-1);
                        if (product.InicioPromocion <= DateTime.Now && DateTime.Now <= fechaFin)
                        {
                            if (product.PermitePromocion == "Si")
                            {
                                decimal promocion = product.Promocion / 100;
                                if (promocion > 0)
                                {
                                    compare_price = (product.PrecioTV * (1 - promocion)).ToString();
                                }
                            }
                        }                        
                        dynamic JProduct = new
                        {
                            variant = new {
                                id = product.Id,
                                price = product.PrecioTV,
                                compare_at_price = compare_price
                            }
                        };
                        IRestResponse response = CallShopify("variants/" + product.Id + ".json", Method.PUT, JProduct);
                        if (response.StatusCode.ToString().Equals("OK"))
                            logger.Info("Product price updated");
                        else
                            logger.Error("Error updating price: " + response.ErrorMessage);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error updating price");
                return;
            }
        }

        public void GetOrders()
        {
            try
            {
                IRestResponse response = CallShopify("orders.json?fulfillment_status=unfulfilled&created_at_min=" + DateTime.Now.AddDays(-20).ToString("yyyy-MM-dd") + "&financial_status=paid&since_id=0", Method.GET, null);
                if(response.StatusCode.ToString().Equals("OK"))
                {
                    MainOrder SO = JsonConvert.DeserializeObject<MainOrder>(response.Content);
                    if(SO.orders != null)
                    {
                        string orders = String.Join(",", SO.orders.Select(r => r.id).ToArray());
                        List<Order> lstOrder = new List<Order>();
                        using (var context = new Models.AppContext(kellyConnStr))
                        {
                            lstOrder = context.Orders.Where(o => o.id.Contains(orders)).ToList();
                            foreach (Order order in SO.orders)
                            {
                                if(lstOrder.Where(or => or.id == order.id).Count() == 0)
                                {
                                    logger.Info("Downloading order: " + order.id);

                                    context.Orders.Add(order);
                                    context.SaveChanges();

                                    response = CallShopify("orders/" + order.id + "/transactions.json", Method.GET,null);

                                    if(response.StatusCode.ToString().Equals("OK"))
                                    {
                                        MasterPayment mp = JsonConvert.DeserializeObject<MasterPayment>(response.Content);
                                        if(mp.transactions != null)
                                        {
                                            foreach(Payment payment in mp.transactions)
                                            {
                                                if(payment.receipt != null)
                                                {
                                                    payment.x_account_id = payment.receipt.x_account_id;
                                                    payment.x_signature = payment.receipt.x_signature;
                                                    payment.x_reference = payment.receipt.x_reference;
                                                }                                                

                                                context.Payment.Add(payment);
                                            }
                                        }
                                    }
                                    
                                    foreach (Item item in order.line_items)
                                    {
                                        item.order_id = order.id;
                                        decimal tax_price = 0;
                                        decimal tax_rate = 0;
                                        foreach (var tax_line in item.tax_lines)
                                        {
                                            tax_price += tax_line.price;
                                            tax_rate += tax_line.rate;
                                        }
                                        item.tax_price = tax_price;
                                        item.tax_rate = tax_rate;
                                        context.Item.Add(item);
                                    }

                                    order.customer.order_id = order.id;
                                    order.customer.default_address.customer_id = order.customer.id;
                                    order.billing_address.order_id = order.id;
                                    order.shipping_address.order_id = order.id;

                                    context.BillAddress.Add(order.billing_address);
                                    context.ShipAddress.Add(order.shipping_address);
                                    context.Customer.Add(order.customer);
                                    context.CustomerAddress.Add(order.customer.default_address);

                                    context.SaveChanges();
                                    logger.Info("Downloading successfully");
                                }
                            }
                        }
                    }
                }
                else
                    logger.Error("Error downloading orders: " + response.ErrorMessage);
            }
            catch(Exception e)
            {
                logger.Error(e, "Error downloading orders");
                return;
            }
        }

        public void UploadProduct()
        {
            try
            {
                List<ProductKelly> lsParent = new List<ProductKelly>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    lsParent = context.ProductKelly.FromSqlInterpolated($"GetProductInfoForShopify {DateTime.Now.AddDays(-10).ToString("yyyy/MM/dd")}").ToList();
                }

                foreach(ProductKelly parent in lsParent)
                {
                    ProductShopify ps = new ProductShopify();

                    ps.title = parent.DescripcionPadre;
                    ps.vendor = parent.Marca;
                    ps.product_type = parent.SegmentoNivel4;
                    ps.body_html = "";
                    ps.tags = parent.SegmentoNivel2 + "," + parent.Color + "," + parent.CodigoProducto + "," + parent.Material + "," + parent.Marca + "," + parent.SegmentoNivel5;
                    ps.handle = parent.CodigoPadre + "-" + parent.SegmentoNivel4 + "-" + parent.SegmentoNivel2 + "-" + parent.Color + "-" + parent.Marca;

                    List<KellyChild> lsChild = new List<KellyChild>();
                    using (var context = new Models.AppContext(kellyConnStr))
                    {
                        lsChild = context.KellyChild.FromSqlInterpolated($"GetProductInfoForShopify {parent.CodigoPadre}").ToList();
                    }

                    string talla = String.Join(",", lsChild.Select(r => r.Talla).ToArray());
                    ps.tags += "," + talla;

                    List<Option> lsOpt = new List<Option>();
                    Option option = new Option();
                    option.name = "Size";
                    option.position = 1;
                    option.values = talla;
                    lsOpt.Add(option);

                    ps.options = lsOpt;

                    foreach (KellyChild child in lsChild)
                    {
                        Variant variant = new Variant();

                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e,"Error updating product information");
                return;
            }
        }

        public IRestResponse CallShopify(string resource,RestSharp.Method method,dynamic parameters)
        {
            try
            {
                Uri url = new Uri(webURL);
                RestClient rest = new RestClient(url);
                rest.Authenticator = new HttpBasicAuthenticator(webAPI, webPassword);

                RestRequest request = new RestRequest(resource, method);
                request.AddHeader("header", "Content-Type: application/json");

                if (parameters != null)
                {
                    dynamic JsonObj = JsonConvert.SerializeObject(parameters);
                    request.AddParameter("application/json", JsonObj, ParameterType.RequestBody);
                }

                IRestResponse response = rest.Execute(request);
                return response;
            }
            catch(Exception e)
            {
                logger.Error(e, "Error calling API");
                return null;
            }
        }
    }
}
/*DataSet ds = new DataSet();
using (var sqlCon = new SqlConnection(kellyConnStr))
{
    SqlCommand sqlCom = new SqlCommand("dbo.GetStockForShopify", sqlCon);
    sqlCom.CommandType = CommandType.StoredProcedure;
    sqlCom.Parameters.Add(new SqlParameter("@FechaStock", DateTime.Now.ToString("yyyy/MM/dd")));

    SqlDataAdapter da = new SqlDataAdapter();
    da.SelectCommand = sqlCom;
    da.Fill(ds);
    da.Dispose();
}*/