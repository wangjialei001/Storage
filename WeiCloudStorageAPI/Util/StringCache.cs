using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Util
{
    public class StringCache
    {
        private readonly RedisHelper _redisHelper;
        private readonly IConfiguration _config;
        public StringCache(RedisHelper redisHelper, IConfiguration config)
        {
            _config = config;
            _redisHelper = redisHelper;
        }
        public T GetValue<T>(string key, int db = 0)
        {
            try
            {
                var dbStr = _config["CacheCluster:DB"];
                if (!int.TryParse(dbStr, out db))
                    db = 0;
                //return _redisHelper.ConnectRedis().Get<T>(key);
                var result = _redisHelper.ConnectRedis().GetDatabase(db).StringGet(key);
                return JsonConvert.DeserializeObject<T>(result);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }
        public string GetValue(string key, int db = 0)
        {
            try
            {
                var dbStr = _config["CacheCluster:DB"];
                if (!int.TryParse(dbStr, out db))
                    db = 0;
                //return _redisHelper.ConnectRedis().Get<T>(key);
                var result = _redisHelper.ConnectRedis().GetDatabase(db).StringGet(key);
                if (result.HasValue)
                    return result.ToString();
                else
                    return string.Empty;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public void SetValue(string key, string obj, int expire = -1, int db = 0)
        {
            try
            {
                //_redisHelper.ConnectRedis().Set(key, obj, expire);
                var dbStr = _config["CacheCluster:DB"];
                if (!int.TryParse(dbStr, out db))
                    db = 0;
                if (expire == -1)
                    _redisHelper.ConnectRedis().GetDatabase(db).StringSet(key, obj);
                else
                    _redisHelper.ConnectRedis().GetDatabase(db).StringSet(key, obj, DateTime.Now.AddSeconds(expire).Subtract(DateTime.Now));
            }
            catch (Exception ex)
            {

            }
        }
        public void SetValue<T>(string key, T obj, int expire = -1, int db = 0)
        {
            try
            {
                //_redisHelper.ConnectRedis().Set(key, obj, expire);
                var dbStr = _config["CacheCluster:DB"];
                if (!int.TryParse(dbStr, out db))
                    db = 0;
                if (expire == -1)
                    _redisHelper.ConnectRedis().GetDatabase(db).StringSet(key, JsonConvert.SerializeObject(obj));
                else
                    _redisHelper.ConnectRedis().GetDatabase(db).StringSet(key, JsonConvert.SerializeObject(obj), DateTime.Now.AddSeconds(expire).Subtract(DateTime.Now));
            }
            catch (Exception ex)
            {

            }
        }
    }
}
