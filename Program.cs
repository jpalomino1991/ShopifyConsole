using System;
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
                string kellyConnString = Environment.GetEnvironmentVariable("KellyConnectionString");

                ShopifyService Sservice = new ShopifyService(kellyConnString, logger);
                switch (args[0])
                {
                    case "/GP":
                        logger.Info("Getting products from shopify " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.GetProducts();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/STOCK":
                        logger.Info("Update Stock start at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.UpdateStock();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/PRICE":
                        logger.Info("Update Price start at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.UpdatePrice();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/ORDER":
                        logger.Info("Download order start at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.GetOrders();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/UPLOAD":
                        logger.Info("Updating product information at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.UploadProduct();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/IMAGE":
                        logger.Info("Updating product information at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.getProductImage();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/PF":
                        logger.Info("Updating product information at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
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
