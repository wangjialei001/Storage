using Msg.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Services
{
    public interface IUniAppMsgService
    {
        Task UniEquipFaultAppPushMsg();
        Task<object> UniEquipFaultAppPushMsg(ProduceMsgModel model);
        Task TestConsumeMsg();
    }
}
