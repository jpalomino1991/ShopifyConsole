using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NLog;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

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
        private string remotePath;
        private string smtpUser;
        private string smtpPass;
        public ShopifyService(string webURL,string webAPI,string webPassword,string kellyConnStr,string locationId,Logger logger)
        {
            this.webURL = webURL;
            this.webAPI = webAPI;
            this.webPassword = webPassword;
            this.kellyConnStr = kellyConnStr;
            this.locationId = locationId;
            this.logger = logger;

            this.remotePath = Environment.GetEnvironmentVariable("RemotePath");
            this.smtpUser = Environment.GetEnvironmentVariable("SMTPUser");
            this.smtpPass = Environment.GetEnvironmentVariable("SMTPPassword");
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
                                InsertProduct(product,context,false);
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

        public void InsertProduct(ProductShopify product,AppContext context,bool inserted)
        {
            try
            {
                logger.Info("Inserting Product " + product.title);                

                Product p = new Product();
                p.Id = product.id;
                p.Title = product.title;
                p.Description = product.body_html;
                p.Vendor = product.vendor;
                p.Handle = product.handle;
                p.ProductType = product.product_type;
                p.Tags = product.tags;
                p.SKU = product.variants[0].sku.Substring(0, product.variants[0].sku.LastIndexOf("."));
                p.Status = product.status;
                p.CreateDate = product.created_at;
                p.UpdateDate = product.updated_at;

                IRestResponse response = CallShopify($"products/{product.id}/metafields.json", Method.GET, null);
                if (response.StatusCode.ToString().Equals("OK"))
                {
                    MasterMetafield mm = JsonConvert.DeserializeObject<MasterMetafield>(response.Content);
                    if (mm.metafields.Count > 0)
                    {
                        p.SEODescription = mm.metafields[0].value;
                        p.SEOTitle = mm.metafields[1].value;
                    }
                }
                else
                    logger.Error("Error updating stock: " + response.ErrorMessage);

                if (inserted)
                    context.Update(p);
                else
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
                    child.CreateDate = variant.created_at;
                    child.UpdateDate = variant.updated_at;

                    if (inserted)
                        context.Update(child);
                    else
                        context.Add(child);
                }
                logger.Info("Product inserted");
            }
            catch (Exception e)
            {
                logger.Error(e, "Error inserting product " + product.id);
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
                    lsStock = context.Stock.FromSqlInterpolated($"GetStockForShopify {DateTime.Now.AddDays(-10).ToString("yyyy/MM/dd")}").ToList();
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
                        string compare_price = GetPromoPrice(product.InicioPromocion,product.FinPromocion,product.PermitePromocion,product.PrecioTV,product.Promocion);
                        dynamic JProduct = new
                        {
                            variant = new {
                                id = product.Id,
                                price = compare_price == "" ? product.PrecioTV : decimal.Parse(compare_price),
                                compare_at_price = compare_price == "" ? compare_price : product.PrecioTV.ToString()
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

        public string GetPromoPrice(string beginDate,string endDate,string isPromo,decimal price,decimal discount)
        {
            string compare_price = "";
            if (string.IsNullOrEmpty(beginDate))
                return compare_price;
            DateTime fechaFin = DateTime.Parse(endDate).AddDays(1).AddTicks(-1);
            if (DateTime.Parse(beginDate) <= DateTime.Now && DateTime.Now <= fechaFin)
            {
                if (isPromo == "Si")
                {
                    decimal promocion = discount / 100;
                    if (promocion > 0)
                    {
                        compare_price = (price * (1 - promocion)).ToString();
                    }
                }
            }

            return compare_price;
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
                    lsParent = context.ProductKelly.FromSqlInterpolated($"GetProductInfoForShopify {DateTime.Now.AddDays(-15).ToString("yyyy/MM/dd")}").ToList();
                }

                foreach(ProductKelly parent in lsParent)
                {
                    ProductShopify ps = new ProductShopify();

                    string body = "<table width='100%'><tbody><tr><td><strong>Color: </strong>Camel</td><td><strong>Marca: </strong>{0}</td><td><strong>Taco:&nbsp;</strong>{1}</td></tr>" +
                        "<tr><td><strong>Material:<span>&nbsp;</span></strong>{2}</td><td><strong>Material Interior:<span>&nbsp;</span></strong>{3}</td><td><strong>Material de Suela:<span>" +
                        "&nbsp;</span></strong>{4}</td></tr><tr><td><strong>Hecho en:<span>&nbsp;</span></strong>{5}</td><td><strong>Modelo:<span>&nbsp;</span></strong>{6}</td><td><br></td>" +
                        "</tr></tbody></table>";
                                                            
                    ps.vendor = parent.Vendor == null ? parent.Marca : parent.Vendor;
                    ps.product_type = parent.ProductType == null ? parent.SegmentoNivel4 : parent.ProductType;
                    ps.body_html = parent.Description == null ? String.Format(body,parent.Marca,parent.Taco,parent.Material,parent.MaterialInterior,parent.MaterialSuela,parent.HechoEn,parent.CodigoProducto) : parent.Description;
                    ps.tags = $"{parent.SegmentoNivel2},{parent.Color},{parent.CodigoProducto},{parent.Material},{parent.Marca},{parent.SegmentoNivel1},{parent.SegmentoNivel4},{parent.SegmentoNivel5},{parent.CodigoPadre}";
                    ps.handle = parent.Handle == null ? $"{parent.CodigoProducto}-{parent.SegmentoNivel4}-{parent.SegmentoNivel2}-{parent.Color}-{parent.Marca}" : parent.Handle;
                    ps.id = parent.Id;
                    string cp = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.CodigoProducto.ToLower());
                    string mat = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Material.ToLower());
                    string col = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Color.ToLower());
                    string mar = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Marca.ToLower());
                    string ven = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Vendor.ToLower());
                    ps.title = $"{parent.SegmentoNivel1} {col} {cp}";
                    ps.metafields_global_description_tag = $"{(parent.Campaña == null ? "" : parent.Campaña + " ")} {parent.SegmentoNivel2} {parent.SegmentoNivel5} {col} {mat} {col} {ven}";
                    ps.metafields_global_title_tag = $"{parent.SegmentoNivel5} {cp} {mat}|{col}|{mar}";

                    List<KellyChild> lsChild = new List<KellyChild>();
                    List<ProductImage> lstImage = new List<ProductImage>();
                    using (var context = new Models.AppContext(kellyConnStr))
                    {
                        lsChild = context.KellyChild.FromSqlInterpolated($"GetProductChildInfo {parent.CodigoPadre}").ToList();
                        lstImage = context.ProductImage.Where(i => i.name.Contains(parent.CodigoPadre)).ToList();
                    }

                    string talla = String.Join(",", lsChild.Select(r => r.Talla).ToArray());
                    ps.tags += "," + talla;

                    List<ImageShopify> imageShopifies = new List<ImageShopify>();

                    if(lstImage.Count > 0)
                    {
                        int i = 1;
                        foreach(ProductImage image in lstImage)
                        {
                            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remotePath + "/" + image.name);
                            request.Method = WebRequestMethods.Ftp.DownloadFile;
                            request.Credentials = new NetworkCredential(smtpUser, smtpPass);
                            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                            Stream responseStream = response.GetResponseStream();
                            byte[] bytes;
                            using (var memoryStream = new MemoryStream())
                            {
                                responseStream.CopyTo(memoryStream);
                                bytes = memoryStream.ToArray();
                            }

                            string img = Convert.ToBase64String(bytes);

                            ImageShopify imgS = new ImageShopify();
                            imgS.attachment = img;
                            imgS.filename = $"{ps.metafields_global_title_tag.ToUpper().Replace(" ","_")}_{i}.jpg";

                            imageShopifies.Add(imgS);
                            i++;
                        }
                    }

                    ps.images = imageShopifies;

                    List<Option> lsOpt = new List<Option>();
                    Option option = new Option();
                    option.name = "Size";
                    option.position = 1;
                    option.values = talla;
                    lsOpt.Add(option);

                    ps.options = lsOpt;

                    List<Variant> lsVariant = new List<Variant>();
                    int stock = 0;

                    foreach (KellyChild child in lsChild)
                    {
                        Variant variant = new Variant();

                        string promoPrice = GetPromoPrice(child.InicioPromocion, child.FinPromocion, child.PermitePromocion, child.PrecioTV, child.Promocion);
                        variant.id = child.Id;
                        variant.sku = child.CodigoSistema;
                        variant.price = promoPrice == "" ? child.PrecioTV : decimal.Parse(promoPrice);
                        variant.option1 = child.Talla.ToString();
                        variant.inventory_quantity = child.StockTotal <= 0 ? 0 : child.StockTotal;
                        variant.inventory_management = "shopify";
                        variant.compare_at_price = promoPrice == "" ? promoPrice : child.PrecioTV.ToString();
                        stock += child.StockTotal;
                        lsVariant.Add(variant);
                    }

                    if (stock <= 0)
                        ps.status = "draft";
                    else
                        ps.status = "active";
                    ps.variants = lsVariant;

                    dynamic oJson = new
                    {
                        product = ps
                    };

                    if(parent.Id != null)
                    {
                        IRestResponse response = CallShopify("products/" + parent.Id + ".json", Method.PUT, oJson);
                        if (response.StatusCode.ToString().Equals("OK"))
                        {
                            MasterProduct mp = JsonConvert.DeserializeObject<MasterProduct>(response.Content);
                            if (mp.product != null)
                            {
                                logger.Info("Uploading product: " + parent.CodigoPadre);
                                using (var context = new Models.AppContext(kellyConnStr))
                                {
                                    InsertProduct(mp.product, context, true);
                                    context.SaveChanges();
                                }
                                logger.Info("Product uploaded");
                            }
                        }                            
                        else
                            logger.Error("Error uploading product: " + response.ErrorMessage);
                    }
                    else
                    {
                        IRestResponse response = CallShopify("products.json", Method.POST, oJson);
                        if (response.StatusCode.ToString().Equals("Created"))
                        {
                            MasterProduct mp = JsonConvert.DeserializeObject<MasterProduct>(response.Content);
                            if(mp.product != null)
                            {
                                logger.Info("Uploading product: " + parent.CodigoPadre);
                                using (var context = new Models.AppContext(kellyConnStr))
                                {
                                    InsertProduct(mp.product, context, false);
                                    context.SaveChanges();
                                }
                                logger.Info("Product uploaded");
                            }
                        }                            
                        else
                            logger.Error("Error uploading product: " + response.ErrorMessage);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e,"Error uploading product");
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

        public void getProductImage()
        {
            try
            {
                logger.Info("Connecting to smtp server");
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remotePath);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(smtpUser, smtpPass);
                logger.Info("Successful");
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                using (var context = new Models.AppContext(kellyConnStr))
                {
                    string line = reader.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        if (line.Contains(".jpg"))
                        {
                            logger.Info($"Getting image: {line}");
                            ProductImage img = new ProductImage();
                            img.name = line;
                            context.ProductImage.Add(img);
                        }
                        line = reader.ReadLine();
                    }
                    context.SaveChanges();
                }

                reader.Close();
                responseStream.Close();
                response.Close();
            }
            catch (Exception e)
            {
                logger.Error(e, "Error getting product image");
                return;
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