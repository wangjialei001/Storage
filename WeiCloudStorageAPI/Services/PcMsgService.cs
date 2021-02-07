using Microsoft.Extensions.Logging;
using Msg.Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Data;
using WeiCloudStorageAPI.Util;

namespace WeiCloudStorageAPI.Services
{
    public class PcMsgService : IPcMsgService
    {
        private readonly DBContext _dbContext;
        private HashCache _hashCache;
        private readonly ILogger<PcMsgService> _logger;
        public PcMsgService(HashCache hashCache, DBContext dbContext, ILogger<PcMsgService> logger)
        {
            _dbContext = dbContext;
            _hashCache = hashCache;
            _logger = logger;
        }
        /// <summary>
        /// 删除所有PC端ClientId
        /// </summary>
        /// <returns></returns>
        public async Task<object> DelAllClientMsgInfo()
        {
            try
            {
                int n = await _dbContext.ExecuteAsync("delete from `UserMapClient` where ClientType=2");
                return new { Code = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError("DelClientMsgInfoByClientId;" + ex.Message);
            }
            return new { Code = 500 };
        }
        public async Task<object> DelClientMsgInfoByClientId(PostClientInfoModel model)
        {
            try
            {
                int n = await _dbContext.ExecuteAsync("delete from `UserMapClient` where ClientId=@ClientId and ClientType=2", new { ClientId = model.ClientId });
                return new { Code = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError("DelClientMsgInfoByClientId;" + ex.Message);
            }
            return new { Code = 500 };
        }
        /// <summary>
        /// 新增客户端和UserId关联
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<object> PostClientMsgInfo(PostClientInfoModel model)
        {
            int count = _dbContext.QueryScalar<int>("select count(1) from `UserMapClient` where UserId=@UserId and ClientId=@ClientId and ClientType=2", new { UserId = model.UserId, ClientId = model.ClientId });
            if (count == 0)
            {
                int n = await _dbContext.ExecuteAsync("insert `UserMapClient` (Id,UserId,ClientId,ClientType) values (@Id,@UserId,@ClientId,@ClientType)", new { Id = UidGenerator.Uid(), ClientType = 2, ClientId = model.ClientId, UserId = model.UserId });
                if (n > 0)
                {
                    var pushMsgClientInfo = await _dbContext.QueryFirstAsync<UserMapClient>("SELECT Id,UserId,ClientId,ClientType FROM `UserMapClient` WHERE UserId=@UserId", new { UserId = model.UserId });
                    _hashCache.SetValue("PushMsgClientInfo", model.UserId.ToString(), JsonConvert.SerializeObject(pushMsgClientInfo));
                    return new { Code = 200 };
                }
                else
                {
                    return new { Code = 500 };
                }
            }
            return new { Code = 200 };
        }
    }
}
