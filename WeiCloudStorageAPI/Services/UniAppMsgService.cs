using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Msg.Core.Enum;
using Msg.Core.Model;
using Msg.Core.MQ;
using Msg.Core.UniPush;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Data;
using WeiCloudStorageAPI.DBModel;
using WeiCloudStorageAPI.Hubs;
using WeiCloudStorageAPI.Util;

namespace WeiCloudStorageAPI.Services
{
    public class UniAppMsgService : IUniAppMsgService
    {
        private readonly UniPushUtil _uniPushUtil;
        private readonly DBContext _dbContext;
        private HashCache _hashCache;
        private readonly IHubContext<MsgHub> _msgHub;
        private readonly ILogger<UniAppMsgService> _logger;
        public UniAppMsgService(HashCache hashCache, UniPushUtil uniPushUtil, DBContext dbContext, IHubContext<MsgHub> msgHub, ILogger<UniAppMsgService> logger)
        {
            _uniPushUtil = uniPushUtil;
            _dbContext = dbContext;
            _hashCache = hashCache;
            _msgHub = msgHub;
            _logger = logger;
        }
        public async Task<object> UniEquipFaultAppPushMsg(ProduceMsgModel model)
        {
            int maxTitleLen = 50;
            int maxBodyLen = 256;
            var userMapClientInfos = await _dbContext.QueryAsync<UserMapClientEntity>("SELECT Id,UserId,ClientId,ClientType FROM `UserMapClient` WHERE UserId IN @ids", new { ids = model.UserIds.ToArray() });
            if (userMapClientInfos == null || userMapClientInfos.Count() == 0)
            {
                return null;
            }
            var clienIds = userMapClientInfos.Where(ui => ui.ClientType == 1).Select(ui => ui.ClientId).Distinct().ToList();
            foreach(var clientId in clienIds)
            {
                await _uniPushUtil.Push1(new MsgPushEntity
                {
                    ClientIds = new List<string> { clientId },
                    Title = !string.IsNullOrEmpty(model.Title) && model.Title.Length > maxTitleLen ? model.Title.Substring(0, maxTitleLen) : model.Title,
                    Body = !string.IsNullOrEmpty(model.Content) && model.Content.Length > maxBodyLen ? model.Content.Substring(0, maxBodyLen) : model.Content,
                    PlayLoad = "\"projectId\":" + model.ProjectId + ",\"id\":" + model.Id + ",\"type\":1"
                });
            }
            return "ok";
            //return await _uniPushUtil.Push2(new MsgPushEntity
            //{
            //    ClientIds = model.UniClinetIds,
            //    Title = !string.IsNullOrEmpty(model.Title) && model.Title.Length > maxTitleLen ? model.Title.Substring(0, maxTitleLen) : model.Title,
            //    ClickType = 2,
            //    Body = !string.IsNullOrEmpty(model.FauDesc) && model.FauDesc.Length > maxBodyLen ? model.FauDesc.Substring(0, maxBodyLen) : model.FauDesc,
            //    ToDo = new List<string> { model.ProjectId.ToString(), model.Id.ToString() }
            //});
        }
        public async Task UniEquipFaultAppPushMsg()
        {
            try
            {
                MQUtil.Consume<ProduceMsgModel>("FaultMsgTopic", (Action<string>)(async (faultMsgStr) =>
                {
                    try
                    {
                        var faultMsg = JsonConvert.DeserializeObject<ProduceMsgModel>(faultMsgStr);
                        IEnumerable<UserMapClientEntity> userMapClientInfos = null;
                        long sendMsgId = UidGenerator.Uid();
                        if (faultMsg == null)
                        {
                            return;
                        }
                        if (faultMsg.UserIds == null || faultMsg.UserIds.Count() == 0)
                        {
                            return;
                        }
                        if (faultMsg.SendTypes == null || faultMsg.SendTypes.Count() == 0)
                        {
                            return;
                        }
                        userMapClientInfos = await this._dbContext.QueryAsync<UserMapClientEntity>("SELECT Id,UserId,ClientId,ClientType FROM `UserMapClient` WHERE UserId IN @ids", new { ids = faultMsg.UserIds.ToArray() });
                        if (userMapClientInfos == null || userMapClientInfos.Count() == 0)
                        {
                            return;
                        }
                        if (faultMsg.SendTypes.Contains((int)SendTypeMeta.AppMsg))
                        {//app端
                            var clienIds = userMapClientInfos.Where(ui => ui.ClientType == (short)SendTypeMeta.AppMsg).Select(ui => ui.ClientId).Distinct().ToList();
                            foreach (var clienId in clienIds)
                            {
                                try
                                {
                                    var pushMsg = new MsgPushEntity
                                    {
                                        ClientIds = new List<string> { clienId },
                                        Title = faultMsg.Title,
                                        //ClickType = 3,
                                        Body = !string.IsNullOrEmpty(faultMsg.Content) ? faultMsg.Content : string.Empty,
                                        //ToDo = new List<string> { "", faultMsg.Id.ToString() }
                                        PlayLoad = "\"projectId\":" + faultMsg.ProjectId + ",\"id\":" + faultMsg.Id + ",\"type\":1" + ",\"msgId\":" + sendMsgId
                                    };
                                    await _uniPushUtil.Push1(pushMsg);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "ActionName:" + this.GetType().FullName);
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }
                        if (faultMsg.SendTypes.Contains((int)SendTypeMeta.PcMsg))
                        {//PC端
                            var pcClienIds = userMapClientInfos.Where(ui => ui.ClientType == (short)SendTypeMeta.PcMsg).Select(ui => ui.ClientId).Distinct().ToList();
                            foreach (var pcClienId in pcClienIds)
                            {
                                try
                                {
                                    await _msgHub.Clients.Client(pcClienId).SendAsync("ReceiveFaultMsg", new MsgPushModel
                                    {
                                        MsgId = sendMsgId,
                                        Title = faultMsg.Title,
                                        Body = !string.IsNullOrEmpty(faultMsg.Content) && faultMsg.Content.Length > 20 ? faultMsg.Content.Substring(0, 20) : faultMsg.Content,
                                        ClientId = pcClienId,
                                        Type = 1,
                                        PlayLoad = faultMsg.Extend
                                    });
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "ActionName:" + this.GetType().FullName);
                                    Console.WriteLine(ex.Message);
                                }
                            }
                        }
                        
                        #region 入库
                        int n = await this._dbContext.ExecuteAsync("INSERT `SendMsgs` (Id,Title,Content,Payload,ProduceContent) VALUES (@Id,@Title,@Content,@Payload,@ProduceContent)",
                                    new { Id = sendMsgId, Title = faultMsg.Title, Content = faultMsg.Content, Payload = JsonConvert.SerializeObject(new { ProjectId = faultMsg.ProjectId, Type = 1, Id = faultMsg.Id }), ProduceContent = JsonConvert.SerializeObject(faultMsg) });
                        if (n > 0 && userMapClientInfos != null && userMapClientInfos.Count() > 0)
                        {
                            foreach (var userInfo in userMapClientInfos)
                            {
                                await this._dbContext.ExecuteAsync("INSERT `SendUserMsg` (Id,UserId,MsgId,IsSend,SendType,IsRead) VALUES (@Id,@UserId,@MsgId,@IsSend,@SendType,@IsRead)",
                                    new { Id = UidGenerator.Uid(), UserId = userInfo.UserId, MsgId = sendMsgId, IsSend = 0, SendType = 1, IsRead = 0 });
                                await this._dbContext.ExecuteAsync("INSERT `SendUserMsg` (Id,UserId,MsgId,IsSend,SendType,IsRead) VALUES (@Id,@UserId,@MsgId,@IsSend,@SendType,@IsRead)",
                                    new { Id = UidGenerator.Uid(), UserId = userInfo.UserId, MsgId = sendMsgId, IsSend = 0, SendType = 2, IsRead = 0 });
                            }
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ActionName:" + this.GetType().FullName);
                        Console.WriteLine(ex.Message);
                    }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ActionName:" + this.GetType().FullName);
            }
            await Task.CompletedTask;
        }


        public async Task TestConsumeMsg()
        {
            try
            {
                MQUtil.Consume1("Test1", (t) =>
                {
                    Console.WriteLine(t);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ActionName:" + this.GetType().FullName);
            }
            await Task.CompletedTask;
        }
    }
}
