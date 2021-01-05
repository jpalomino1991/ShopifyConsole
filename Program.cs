using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NLog;
using ShopifyConsole.Models;

namespace ShopifyConsole
{
    class Program
    {        
        static void Main(string[] args)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            try
            {
                JObject o1 = JObject.Parse(File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}/appSettings.json"));
                string kellyConnString = o1.First.First.ToString();

                ShopifyService Sservice = new ShopifyService(kellyConnString, logger);
                int result = 0;
                switch (args[0])
                {
                    case "/GP":
                        logger.Info("Getting products from shopify " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.GetProducts();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/STOCK":
                        logger.Info("Update Stock start at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.UpdateStock(result);
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/PRICE":
                        logger.Info("Update Price start at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.UpdatePrice(result);
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/ORDER":
                        logger.Info("Download order start at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.GetOrders(result);
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/UPLOAD":
                        logger.Info("Updating product information at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.UploadProduct(result,false);
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/ALL":
                        logger.Info("Updating product information at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.UploadProduct(result,true);
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/IMAGE":
                        logger.Info("Getting product image at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.getProductImage();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/PF":
                        logger.Info("Getting product filters at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.getProductFilter();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }            
        }
    }
}
