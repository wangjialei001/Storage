using Msg.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Services
{
    public interface IPcMsgService
    {
        Task<object> PostClientMsgInfo(PostClientInfoModel model);
        Task<object> DelClientMsgInfoByClientId(PostClientInfoModel model);
        Task<object> DelAllClientMsgInfo();
    }
}
