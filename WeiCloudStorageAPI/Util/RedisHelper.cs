using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;

namespace WeiCloudStorageAPI.Util
{
    public class RedisHelper
    {
        private readonly IConfigurationRoot _config;
        private readonly ConnectionMultiplexer _connection;
        public RedisHelper()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var cacheHosts = _config["CacheCluster:Host"].Split(",");
            var cacheConfig = new ConfigurationOptions
            {
                Password = _config["CacheCluster:Pwd"]
            };
            foreach (var cacheHost in cacheHosts)
            {
                cacheConfig.EndPoints.Add(cacheHost);
            }
            
            _connection = ConnectionMultiplexer.Connect(cacheConfig);
            Console.WriteLine("RedisHelper初始化");
        }
        public ConnectionMultiplexer ConnectRedis()
        {
            return _connection;
        }
    }
}
