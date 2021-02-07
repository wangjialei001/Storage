using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Msg.Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Services;
using WeiCloudStorageAPI.Util;

namespace WeiCloudStorageAPI.Controllers
{
    [Route("msg/[controller]/[action]")]
    [ApiController]
    [AllowAnonymous]
    public class AppMsgController : ControllerBase
    {
        private readonly IUniAppMsgService _uniAppMsgService;
        public AppMsgController(IUniAppMsgService uniAppMsgService)
        {
            _uniAppMsgService = uniAppMsgService;
        }
        //[HttpPost]
        //public async Task<object> PostClientMsgInfo(PostClientInfoModel model)
        //{
        //    return await _uniAppMsgService.PostClientMsgInfo(model);
        //}
        [HttpPost]
        public async Task<object> SendClientMsgInfo(ProduceMsgModel model)
        {
            return await _uniAppMsgService.UniEquipFaultAppPushMsg(model);
        }
    }
}
