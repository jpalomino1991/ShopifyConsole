using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Text.RegularExpressions;

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
        public ShopifyService(string kellyConnStr,Logger logger)
        {            
            this.kellyConnStr = kellyConnStr;            
            this.logger = logger;

            using (var context = new Models.AppContext(kellyConnStr))
            {
                Web web = context.Web.Find(1);
                this.remotePath = web.SMTPURL;
                this.smtpUser = web.SMTPUser;
                this.smtpPass = web.SMTPPassword;
                this.webURL = web.WebURL;
                this.webAPI = web.WebAPI;
                this.webPassword = web.WebPassword;
                this.locationId = web.LocationId;
            }                
        }
        public void GetProducts()
        {
            try
            {
                logger.Info("Deleting table Product");
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    context.Database.ExecuteSqlInterpolated($"TRUNCATE TABLE Product");
                    context.Database.ExecuteSqlInterpolated($"TRUNCATE TABLE ProductImage");
                }
                logger.Info("Delete Successfully");
                IRestResponse response = CallShopify("products/count.json", Method.GET, null);
                int pages = 0;
                if (response.StatusCode.ToString().Equals("OK"))
                {
                    JObject res = JObject.Parse(response.Content);
                    pages = (int)res["count"];
                }

                int totalPages = (pages / 250) + ((pages % 250) > 0 ? 1 : 0);
                string pageInfo = "";

                for(int i = 0;i < totalPages;i++)
                {
                    response = CallShopify($"products.json?limit=250&page_info={pageInfo}", Method.GET, null);

                    if (response.StatusCode.ToString().Equals("OK"))
                    {
                        string header = response.Headers[17].Value.ToString();
                        
                        foreach(string content in header.Split(","))
                        {
                            if(content.Contains("next"))
                            {
                                pageInfo = content.Split(";")[0].TrimStart('<').TrimEnd('>').Split("page_info=")[1];
                            }
                        }

                        MasterProduct mp = JsonConvert.DeserializeObject<MasterProduct>(response.Content);
                        if (mp != null)
                        {
                            using (var context = new Models.AppContext(kellyConnStr))
                            {
                                foreach (ProductShopify product in mp.products)
                                {
                                    InsertProduct(product, context, false);
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.Error("Error getting products: " + response.ErrorMessage);
                    }
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
                    context.Product.Update(p);
                else
                    context.Product.Add(p);

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
                        context.Product.Update(child);
                    else
                        context.Product.Add(child);
                }

                foreach(ImageShopify img in product.images)
                {
                    ProductImage pi = new ProductImage();
                    pi.id = img.id;
                    pi.product_id = product.id;
                    pi.src = img.src;
                    pi.alt = img.alt;

                    if (inserted)
                        context.ProductImage.Update(pi);
                    else
                        context.ProductImage.Add(pi);
                }

                context.SaveChanges();
                logger.Info("Product inserted");
            }
            catch (Exception e)
            {
                logger.Error(e, "Error inserting product " + product.id);
                return;
            }            
        }

        public void UpdateStock(int days)
        {
            try
            {
                List<Stock> lsStock = new List<Stock>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    lsStock = context.Stock.FromSqlInterpolated($"GetStockForShopify {DateTime.Now.AddDays(days * -1).ToString("yyyy/MM/dd")}").ToList();
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
                        {
                            logger.Error("Error updating stock: " + response.ErrorMessage);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                logger.Error(e, "Error updating stock");
                return;
            }
        }

        public void UpdatePrice(int days)
        {
            try
            {
                List<Price> lsPrice = new List<Price>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    lsPrice = context.Price.FromSqlInterpolated($"GetPriceForShopify {DateTime.Now.AddDays(days * -1).ToString("yyyy/MM/dd")}").ToList();
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
                        {
                            logger.Error("Error updating price: " + response.ErrorMessage);
                        }
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

        public void GetOrders(int days)
        {
            try
            {
                IRestResponse response = CallShopify("orders.json?fulfillment_status=unfulfilled&created_at_min=" + DateTime.Now.AddDays(days * -1).ToString("yyyy-MM-dd") + "&since_id=0", Method.GET, null);
                if(response.StatusCode.ToString().Equals("OK"))
                {
                    MainOrder SO = JsonConvert.DeserializeObject<MainOrder>(response.Content);
                    if(SO.orders != null)
                    {
                        foreach (Order order in SO.orders)
                        {
                            using (var context = new Models.AppContext(kellyConnStr))
                            {
                                Order so = context.Orders.Find(order.id);
                                if(so == null)
                                {
                                    logger.Info("Downloading order: " + order.id);

                                    Customer customer = context.Customer.Find(order.customer.id);

                                    if (customer == null)
                                    {
                                        context.Customer.Add(order.customer);
                                        context.SaveChanges();
                                    }
                                    else
                                        context.Customer.Update(customer);

                                    order.customer_id = order.customer.id;
                                    context.Orders.Add(order);
                                    context.SaveChanges();

                                    response = CallShopify("orders/" + order.id + "/transactions.json", Method.GET, null);

                                    if (response.StatusCode.ToString().Equals("OK"))
                                    {
                                        MasterPayment mp = JsonConvert.DeserializeObject<MasterPayment>(response.Content);
                                        if (mp.transactions != null)
                                        {
                                            foreach (Payment payment in mp.transactions)
                                            {
                                                if (payment.receipt != null)
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

                                    order.customer.default_address.customer_id = order.customer.id;
                                    order.billing_address.order_id = order.id;
                                    order.shipping_address.order_id = order.id;

                                    context.BillAddress.Add(order.billing_address);
                                    context.ShipAddress.Add(order.shipping_address);                                    

                                    CustomerAddress customerAddress = context.CustomerAddress.Find(order.customer.default_address.id);

                                    if(customerAddress == null)
                                        context.CustomerAddress.Add(order.customer.default_address);
                                    else
                                        context.CustomerAddress.Update(customerAddress);

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

        public void UploadProduct(int days,bool all)
        {
            try
            {
                List<ProductKelly> lsParent = new List<ProductKelly>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    if(all)
                        lsParent = context.ProductKelly.FromSqlInterpolated($"GetProductInfoAllForShopify").ToList();
                    else
                        lsParent = context.ProductKelly.FromSqlInterpolated($"GetProductInfoForShopify @FechaProducto = {DateTime.Now.AddDays(days * -1).ToString("yyyy/MM/dd")}").ToList();
                }

                foreach(ProductKelly parent in lsParent)
                {
                    ProductShopify ps = new ProductShopify();

                    string body = "<table width='100%'><tbody><tr><td><strong>Color: </strong>{0}</td><td><strong>Marca: </strong>{1}</td><td><strong>Taco:&nbsp;</strong>{2}</td></tr>" +
                        "<tr><td><strong>Material:<span>&nbsp;</span></strong>{3}</td><td><strong>Material Interior:<span>&nbsp;</span></strong>{4}</td><td><strong>Material de Suela:<span>" +
                        "&nbsp;</span></strong>{5}</td></tr><tr><td><strong>Hecho en:<span>&nbsp;</span></strong>{6}</td><td><strong>Modelo:<span>&nbsp;</span></strong>{7}</td><td><br></td>" +
                        "</tr></tbody></table>";

                    string cp = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.CodigoProducto.ToLower());
                    string mat = parent.Material != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(' ',parent.Material.ToLower().Split('/').ToList())) : "";
                    string matI = parent.MaterialInterior != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(',', parent.MaterialInterior.ToLower().Split('/').ToList())) : "";
                    string matS = parent.MaterialSuela != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(string.Join(',', parent.MaterialSuela.ToLower().Split('/').ToList())) : "";
                    string col = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Color.ToLower());
                    string mar = parent.Marca != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Marca.ToLower()) : "";
                    string oca = parent.Ocasion != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Ocasion.ToLower()) : "";
                    string ten = parent.Tendencia != null ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parent.Tendencia.ToLower()) : "";
                    string imageName = "";
                    string sex = parent.SegmentoNivel2;
                    if (parent.SegmentoNivel2 == "Mujer" && parent.SegmentoNivel3 == "Niña")
                        sex = "Niñas";
                    if (parent.SegmentoNivel2 == "Hombre" && parent.SegmentoNivel3 == "Niño")
                        sex = "Niños";

                    ps.vendor = mar;
                    if(parent.SegmentoNivel4 == "Zapatos")
                    {
                        if (parent.SegmentoNivel5 == "Stiletto")
                            ps.product_type = "Stilettos";
                        if (parent.SegmentoNivel5 == "Fiesta" || parent.SegmentoNivel5 == "Vestir")
                            ps.product_type = $"{parent.SegmentoNivel4} de {parent.SegmentoNivel5}";
                        else
                            ps.product_type = $"{parent.SegmentoNivel4} {parent.SegmentoNivel5}";
                    }
                    else
                        ps.product_type = parent.SegmentoNivel4;
                    ps.body_html = String.Format(body,col,mar,parent.Taco,mat,matI,matS,parent.HechoEn,cp);
                    ps.tags = String.IsNullOrEmpty(parent.Tags) ? $"{ps.product_type},{mat},{col},{cp},{mat.Replace(' ',',')},{mar},{parent.SegmentoNivel1},{(sex != parent.SegmentoNivel2 ? "Kids," + sex : sex)},{parent.SegmentoNivel4},{parent.CodigoPadre},{ten},{oca},{parent.Taco}" : parent.Tags;
                    ps.handle = $"{cp}-{parent.SegmentoNivel4}-{sex}-{col}-{mar}";
                    ps.id = parent.Id;
                    if(parent.SegmentoNivel4 == "Pantuflas" || parent.SegmentoNivel4 == "Alpargatas")
                    {
                        ps.title = $"{parent.SegmentoNivel4} {col} {cp}";
                        ps.metafields_global_description_tag = $"{ten} {oca} {(parent.Campaña == null ? "" : parent.Campaña)} {sex} {parent.SegmentoNivel4} {cp} {mat} {col} {mar}";
                        ps.metafields_global_title_tag = $"{parent.SegmentoNivel4} {cp} {mat} | {col} | {mar}";
                        imageName = $"{parent.SegmentoNivel4}_{cp}_{mat}_{col}_{mar}";
                    }
                    else
                    {
                        if (parent.SegmentoNivel5 == "Stiletto")
                        {
                            ps.title = $"Stilettos {col} {cp}";
                            ps.metafields_global_description_tag = $"{ten} {oca} {(parent.Campaña == null ? "" : parent.Campaña)} {sex} Stilettos {cp} {mat} {col} {mar}";
                            ps.metafields_global_title_tag = $"Stilettos {cp} {mat} | {col} | {mar}";
                            imageName = $"{parent.SegmentoNivel4}_{parent.SegmentoNivel5}_{cp}_{mat}_{col}_{mar}";
                        }
                        else
                        {
                            ps.title = $"{parent.SegmentoNivel4} {parent.SegmentoNivel5} {col} {cp}";
                            ps.metafields_global_description_tag = $"{ten} {oca} {(parent.Campaña == null ? "" : parent.Campaña)} {sex} {parent.SegmentoNivel4} {parent.SegmentoNivel5} {cp} {mat} {col} {mar}";
                            ps.metafields_global_title_tag = $"{parent.SegmentoNivel4} {parent.SegmentoNivel5} {cp} {mat} | {col} | {mar}";
                            imageName = $"{parent.SegmentoNivel4}_{parent.SegmentoNivel5}_{cp}_{mat}_{col}_{mar}";
                        }                            
                    }

                    ps.metafields_global_description_tag = ps.metafields_global_description_tag.Trim();
                    List<KellyChild> lsChild = new List<KellyChild>();
                    List<ProductTempImage> lstImage = new List<ProductTempImage>();
                    List<ProductImage> lstUpload = new List<ProductImage>();
                    using (var context = new Models.AppContext(kellyConnStr))
                    {
                        lsChild = context.KellyChild.FromSqlInterpolated($"GetProductChildInfo {parent.CodigoPadre}").ToList();
                        lstUpload = context.ProductImage.Where(i => i.product_id == parent.Id).ToList();
                        if(lstUpload.Count == 0)
                            lstImage = context.ProductTempImage.Where(i => i.sku == parent.CodigoPadre).OrderBy(i => i.name).ToList();
                    }

                    string talla = String.Join(",", lsChild.Select(r => r.Talla).ToArray());
                    ps.tags += "," + talla;

                    List<ImageShopify> imageShopifies = new List<ImageShopify>();

                    if(lstImage.Count > 0)
                    {
                        int i = 1;
                        foreach(ProductTempImage image in lstImage)
                        {
                            imageShopifies.Add(getImageFromFtp(image,imageName,i));
                            i++;
                        }
                    }

                    ps.images = imageShopifies;

                    List<Option> lsOpt = new List<Option>();
                    Option option = new Option();
                    option.name = "Talla";
                    option.position = 1;
                    //option.values = talla;
                    lsOpt.Add(option);

                    ps.options = lsOpt;

                    List<Variant> lsVariant = new List<Variant>();
                    int stock = 0;

                    foreach (KellyChild child in lsChild)
                    {
                        if (child.PrecioTV == 0) continue;
                        Variant variant = new Variant();

                        string promoPrice = GetPromoPrice(child.InicioPromocion, child.FinPromocion, child.PermitePromocion, child.PrecioTV, child.Promocion);
                        variant.id = child.Id;
                        variant.sku = child.CodigoSistema;
                        variant.price = promoPrice == "" ? child.PrecioTV : decimal.Parse(promoPrice);
                        variant.option1 = child.Talla.ToString();
                        variant.inventory_quantity = child.StockTotal <= 0 ? 0 : child.StockTotal;
                        variant.inventory_management = "shopify";
                        variant.compare_at_price = promoPrice == "" ? promoPrice : child.PrecioTV.ToString();
                        stock += child.StockTotal < 0 ? 0 : child.StockTotal;
                        lsVariant.Add(variant);
                    }

                    if (lsVariant.Count == 0) continue;

                    if (stock <= 0 || (lstImage.Count == 0 && lstUpload.Count == 0))
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
                                }
                                logger.Info("Product uploaded");
                            }
                        }                            
                        else
                            logger.Error("Error uploading product to shopify: " + response.Content);
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
                            logger.Error("Error uploading product to shopify: " + response.Content);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e,"Error uploading product in general");
                return;
            }
        }

        public ImageShopify getImageFromFtp(ProductTempImage image,string imageName,int i)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{remotePath}/{image.name}{image.extension}");
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
                imgS.filename = $"{imageName.ToUpper()}_{i}.jpg";
                imgS.alt = $"{imageName.ToUpper()}_{i}.jpg";

                return imgS;
            }
            catch (Exception e)
            {
                if (e.Message == "Unable to connect to the remote server")
                {
                    return getImageFromFtp(image,imageName,i);
                }
                logger.Error(e, "Error in ftp");
                return null;
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

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || response.StatusCode.ToString().Equals("520"))
                {
                    System.Threading.Thread.Sleep(5000);
                    return CallShopify(resource,method,parameters);
                }

                return response;
            }
            catch(Exception e)
            {
                logger.Error(e, "Error calling API");
                return null;
            }
        }

        public void GetProductImage()
        {
            try
            {
                logger.Info("Connecting to smtp server");
                logger.Info(remotePath);
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(remotePath);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(smtpUser, smtpPass);
                logger.Info("Successful");
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                using (var context = new Models.AppContext(kellyConnStr))
                {
                    context.Database.ExecuteSqlInterpolated($"TRUNCATE TABLE ProductTempImage");
                    string line = reader.ReadLine();
                    while (!string.IsNullOrEmpty(line))
                    {
                        if (line.Contains(".jpg") || line.Contains(".png"))
                        {
                            FileInfo fileInfo = new FileInfo(line);
                            logger.Info($"Getting image: {line}");
                            ProductTempImage img = new ProductTempImage();
                            img.name = Path.GetFileNameWithoutExtension(fileInfo.Name);
                            if (img.name.IndexOf('-') > 0)
                                img.sku = img.name.Substring(0, img.name.IndexOf('-'));
                            else
                                img.sku = img.name;
                            img.extension = fileInfo.Extension;
                            context.ProductTempImage.Add(img);
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

        public void GetProductFilter()
        {
            try
            {
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    logger.Info("Deleting filter table Product");
                    context.Brand.RemoveRange(context.Brand);
                    context.ProductType.RemoveRange(context.ProductType);
                    context.SaveChanges();
                    logger.Info("Delete Successfully");

                    List<Filter> lstFilter = new List<Filter>();

                    lstFilter = context.Filter.FromSqlInterpolated($"GetProductFilterValues Marca").ToList();

                    foreach(Filter filter in lstFilter)
                    {
                        Brand brand = new Brand();
                        brand.Name = filter.Value;
                        context.Brand.Add(brand);
                    }

                    lstFilter = context.Filter.FromSqlInterpolated($"GetProductFilterValues SegmentoNivel4").ToList();

                    foreach (Filter filter in lstFilter)
                    {
                        ProductType type = new ProductType();
                        type.Name = filter.Value;
                        context.ProductType.Add(type);
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error getting products");
                return;
            }
        }

        public void DeleteDuplicate()
        {
            try
            {
                List<Sku> lstDup = new List<Sku>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    lstDup = context.Sku.FromSqlInterpolated($"GetDuplicateProduct").ToList();

                    foreach (Sku sku in lstDup)
                    {
                        char[] numbers = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                        List<Product> lstProd = context.Product.Where(p => p.SKU.Equals(sku.sku)).ToList();
                        foreach(Product product in lstProd)
                        {
                            if(Regex.IsMatch(product.Handle.Substring(product.Handle.Length - 1),@"\d+"))
                            {
                                context.Database.ExecuteSqlInterpolated($"DeleteProduct {product.Id}");
                                IRestResponse response = CallShopify($"products/{product.Id}.json", Method.DELETE, null);
                                if (response.StatusCode.ToString().Equals("OK"))
                                {
                                    logger.Info($"Product {sku.sku} deleted successfully");
                                }
                                else
                                    logger.Error("Error deleting product: " + response.Content);
                            }                            
                        }
                    }
                }                
            }
            catch (Exception e)
            {
                logger.Error(e, "Error on delete product process");
                return;
            }
        }

        public void DeleteMissing()
        {
            try
            {
                List<Sku> lstDup = new List<Sku>();
                using (var context = new Models.AppContext(kellyConnStr))
                {
                    lstDup = context.Sku.FromSqlInterpolated($"GetMissingProduct").ToList();

                    foreach (Sku sku in lstDup)
                    {
                        Product product = context.Product.Where(p => p.SKU == sku.sku).FirstOrDefault();
                        context.Database.ExecuteSqlInterpolated($"DeleteProduct {product.Id}");
                        IRestResponse response = CallShopify($"products/{product.Id}.json", Method.DELETE, null);
                        if (response.StatusCode.ToString().Equals("OK"))
                        {
                            logger.Info($"Product {sku.sku} deleted successfully");
                        }
                        else
                            logger.Error("Error deleting product: " + response.Content);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error on delete product process");
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