using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Msg.Core.UniPush;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Services;

namespace WeiCloudStorageAPI.Hubs
{
    public class MsgHub : Hub
    {
        private readonly ILogger<MsgHub> _logger;
        private readonly IPcMsgService _pcMsg;
        public MsgHub(ILogger<MsgHub> logger, IPcMsgService pcMsg)
        {
            _logger = logger;
            _pcMsg = pcMsg;
        }
        public override Task OnConnectedAsync()
        {
            try
            {
                var userIdStr = Context.GetHttpContext().Request.Query["userId"].ToString();
                long userId = 0;
                if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out userId))
                {
                    _pcMsg.PostClientMsgInfo(new Msg.Core.Model.PostClientInfoModel { ClientId = Context.ConnectionId, UserId = userId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("OnConnectedAsync:" + ex.Message);
            }
            Console.WriteLine("已连接：" + Context.ConnectionId);
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                _pcMsg.DelClientMsgInfoByClientId(new Msg.Core.Model.PostClientInfoModel { ClientId = Context.ConnectionId });
            }
            catch (Exception ex)
            {
                _logger.LogError("OnDisconnectedAsync:" + ex.Message);
            }
            Console.WriteLine("断开连接：" + Context.ConnectionId);

            return base.OnDisconnectedAsync(exception);
        }
    }
}
