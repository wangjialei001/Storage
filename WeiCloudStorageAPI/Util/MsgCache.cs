using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Util
{
    public class MsgCache
    {
        private readonly RedisHelper _redisHelper;
        private readonly IConfiguration _config;
        public MsgCache(RedisHelper redisHelper, IConfiguration config)
        {
            _config = config;
            _redisHelper = redisHelper;
        }

        public async Task Pub(string channel, string msg)
        {
            try
            {
                int db = 0;
                var dbStr = _config["CacheCluster:DB"];
                if (!int.TryParse(dbStr, out db))
                    db = 0;
                await _redisHelper.ConnectRedis().GetSubscriber().PublishAsync(channel, msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public async Task Sub(string channel, Action<string> action)
        {
            int db = 0;
            var dbStr = _config["CacheCluster:DB"];
            if (!int.TryParse(dbStr, out db))
                db = 0;
            await _redisHelper.ConnectRedis().GetSubscriber().SubscribeAsync(channel, (ch, message) =>
            {
                action(message);
            });
        }
    }
}
