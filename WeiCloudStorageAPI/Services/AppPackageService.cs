using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Data;
using WeiCloudStorageAPI.DBModel;
using WeiCloudStorageAPI.Model;
using WeiCloudStorageAPI.Util;

namespace WeiCloudStorageAPI.Services
{
    public class AppPackageService : IAppPackageService
    {
        private readonly DBContext _dbContext;
        private readonly HashCache _hashCache;
        private readonly StringCache _stringCache;
        private readonly ILogger<IAppPackageService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AppPackageService(HashCache hashCache, StringCache stringCache, DBContext dbContext, ILogger<IAppPackageService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _hashCache = hashCache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _stringCache = stringCache;
        }
        /// <summary>
        /// 统计下载数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ts">针对微信打开链接，多次触发下载</param>
        /// <returns></returns>
        public async Task<AppPackagesEntity> StatisticsDownloadCount(string name, long ts = 0)
        {
            int downLoadCount = 0;
            try
            {
                HttpContextAccessor context = new HttpContextAccessor();
                //var ip = context.HttpContext?.Connection.RemoteIpAddress.ToString();
                //Console.WriteLine("ip address：" + ip);

                //string ipaddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                //Console.WriteLine("ipaddress：" + ipaddress);

                var appPackageCache = _hashCache.GetValue<AppPackagesEntity>("AppPackages", name);

                if (appPackageCache == null)
                {
                    var appPackage = await _dbContext.QueryFirstAsync<AppPackagesEntity>("SELECT * FROM `AppPackages` WHERE `isdeleted`=0 AND `packageurl`='" + name + "'");
                    appPackageCache = appPackage;
                }
                if (ts > 0)
                {
                    var tsStr = _stringCache.GetValue(ts.ToString(), 1);
                    if (!string.IsNullOrEmpty(tsStr))
                    {
                        Console.WriteLine(ts.ToString()+"属于重复下载");
                        return appPackageCache;
                    }
                    else
                    {
                        Console.WriteLine(ts.ToString() + "下载");
                        _stringCache.SetValue(ts.ToString(), appPackageCache.DownCount.ToString(), 60 * 5, 1);
                    }
                }
                appPackageCache.DownCount = appPackageCache.DownCount + 1;
                downLoadCount = appPackageCache.DownCount;
                _hashCache.SetValue("AppPackages", name, JsonConvert.SerializeObject(appPackageCache));
                return appPackageCache;
            }
            catch (Exception ex)
            {
                _logger.LogError("StatisticsDownloadCount;" + ex.Message);
            }
            return null;
        }
        public async Task<AppPackagesModel> GetNewestAppPackage(int terminalType, int upgradeType = 0)
        {
            try
            {
                string sql = "SELECT * FROM `AppPackages` WHERE `isdeleted`=0 AND `terminaltype`=" + terminalType;
                if (upgradeType > 0)
                {
                    sql += " AND `upgradetype`=" + upgradeType;
                }
                sql += " ORDER BY `version` DESC";
                var appPackages = await _dbContext.QueryFirstAsync<AppPackagesModel>(sql);
                return appPackages;
            }
            catch (Exception ex)
            {
                _logger.LogError("StatisticsDownloadCount;" + ex.Message);
            }
            return null;
        }
    }
}
