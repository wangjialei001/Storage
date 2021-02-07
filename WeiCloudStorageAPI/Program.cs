using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Msg.Core.MQ;
using NLog.Web;
using WeiCloudStorageAPI.Services;

namespace WeiCloudStorageAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // NLog: setup the logger first to catch all errors
            var logger = NLog.Web.NLogBuilder.ConfigureNLog("NLog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                //.AddEnvironmentVariables()
                .Build();

                IWebHost webHost = CreateWebHostBuilder(args).Build();
                var isConsume = configuration["IsConsume"];
                if (!string.IsNullOrEmpty(isConsume) && isConsume == "1")
                {
                    var uniAppMsg = webHost.Services.CreateScope().ServiceProvider.GetService<IUniAppMsgService>();
                    Thread th1 = new Thread(() =>
                    {
                        try
                        {
                            uniAppMsg.UniEquipFaultAppPushMsg().Wait();
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "初始化失败！");
                        }
                    });
                    th1.Start();
                    Thread th2 = new Thread(() =>
                    {
                        try
                        {
                            uniAppMsg.TestConsumeMsg().Wait();
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "初始化失败！");
                        }
                    });
                    th2.Start();

                    Task.Run(() =>
                    {
                        var pcMsgService = webHost.Services.CreateScope().ServiceProvider.GetService<IPcMsgService>();
                        pcMsgService.DelAllClientMsgInfo().Wait();
                    });
                }
                webHost.Run();
                //webHost.RunAsService();
            }
            catch (Exception ex)
            {
                //NLog: catch setup errors
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            //var pathToContentRoot = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //Console.WriteLine(pathToContentRoot);
            return WebHost.CreateDefaultBuilder(args)
                //.UseContentRoot(pathToContentRoot)
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                })
                .UseNLog();
        }
    }
}
