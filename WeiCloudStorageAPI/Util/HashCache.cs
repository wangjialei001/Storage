using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Util
{
    public class HashCache
    {
        private readonly RedisHelper _redisHelper;
        private readonly IConfiguration _config;
        public HashCache(RedisHelper redisHelper, IConfiguration config)
        {
            _config = config;
            _redisHelper = redisHelper;
        }
        public List<T> GetValue<T>(string hashid)
        {
            int db = 0;
            var dbStr = _config["CacheCluster:DB"];
            if (!int.TryParse(dbStr, out db))
                db = 0;
            var values = _redisHelper.ConnectRedis().GetDatabase(db).HashValues(hashid);
            var items = new List<T>();
            if (values != null && values.Count() > 0)
            {
                foreach (var value in values)
                {
                    var item = JsonConvert.DeserializeObject<T>(value.ToString());
                    items.Add(item);
                }
            }
            return items;
        }
        public T GetValue<T>(string hashid, string key)
        {
            int db = 0;
            var dbStr = _config["CacheCluster:DB"];
            if (!int.TryParse(dbStr, out db))
                db = 0;
            var r = _redisHelper.ConnectRedis().GetDatabase(db).HashGet(hashid, key);
            if (!r.IsNullOrEmpty)
            {
                return JsonConvert.DeserializeObject<T>(r.ToString());
            }
            return default(T);
        }
        public void SetValue(string hashid, string key, string value)
        {
            int db = 0;
            var dbStr = _config["CacheCluster:DB"];
            if (!int.TryParse(dbStr, out db))
                db = 0;
            var r = _redisHelper.ConnectRedis().GetDatabase(db).HashSet(hashid, key, value);
        }
    }
}
