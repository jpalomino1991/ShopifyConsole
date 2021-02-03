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
                        Sservice.GetProducts();
                        break;
                    case "/STOCK":
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.UpdateStock(result);
                        break;
                    case "/PRICE":
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.UpdatePrice(result);
                        break;
                    case "/ORDER":
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.GetOrders(result);
                        break;
                    case "/UPLOAD":
                        logger.Info("Updating product information at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        if (args.Length > 1)
                            result = int.Parse(args[1]);
                        Sservice.UploadProduct(result,false);
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/ALL":
                        //Sservice.GetProductImage();
                        //Sservice.GetProducts();
                        Sservice.UploadProduct(result,true);
                        break;
                    case "/IMAGE":
                        Sservice.GetProductImage();
                        break;
                    case "/PF":
                        logger.Info("Getting product filters at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.GetProductFilter();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/DELETE":
                        logger.Info("Deleting products at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        Sservice.DeleteDuplicate();
                        logger.Info("Finished at " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                        break;
                    case "/MISSING":
                        Sservice.DeleteMissing();
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
