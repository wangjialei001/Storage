using HaikangSDK;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Data;
using WeiCloudStorageAPI.Util;

namespace WeiCloudStorageAPI.Hubs
{
    public class VideoHub: Hub
    {
        private readonly ILogger<VideoHub> _logger;
        private readonly DBContext _dBContext;
        private readonly DBContextAir _dBContextAir;
        private readonly IConfiguration _config;
        private readonly MsgCache _msgCache;
        public VideoHub(ILogger<VideoHub> logger, DBContext dBContext, DBContextAir dBContextAir, IConfiguration config, MsgCache msgCache)
        {
            _logger = logger;
            _dBContext = dBContext;
            _dBContextAir = dBContextAir;
            _config = config;
            _msgCache = msgCache;
        }
        public override Task OnConnectedAsync()
        {
            Console.WriteLine("已连接：" + Context.ConnectionId);
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine("断开连接：" + Context.ConnectionId);
            try
            {
                HaiKangFactory.CustomerClose(Context.ConnectionId);

                return base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("OnDisconnectedAsync", ex.Message);
            }
            return Task.CompletedTask;
        }

        public async Task GetVideo(long equipId)
        {
            var connectionId = Context.ConnectionId;

            var client = Clients.Client(connectionId);
            var isNeedPush = _config["IsVideoNeedPush"];
            var isNeedPull = _config["IsVideoNeedPull"];
            try
            {
                var config = await GetEquipConfig(equipId);
                if (config == null)
                {
                    await client.SendAsync("ShowVideo", new { Success = false, Msg = "用户基本信息不存在！" });
                    return;
                }

                Console.WriteLine(connectionId + "开始连接");
                if (!string.IsNullOrEmpty(isNeedPull) && isNeedPull == "1")
                {
                    await _msgCache.Sub(equipId + "_Video", (t) =>
                    {
                        client.SendAsync("ShowVideo", new { Success = true, Data = t });
                    });
                }
                else
                {
                    HaiKangFactory.Login(equipId.ToString(), config, connectionId);
                    HaiKangFactory.GetVideo(equipId.ToString(), connectionId, (t) =>
                    {
                        if (!string.IsNullOrEmpty(isNeedPush) && isNeedPush == "1")
                        {
                            _msgCache.Pub(equipId + "_Video", t);
                        }
                        client.SendAsync("ShowVideo", new { Success = true, Data = t });
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("GetVideo", ex.Message);
            }
            await Task.CompletedTask;
        }
        private async Task<VideoBaseConfig> GetEquipConfig(long equipId)
        {
            var configsStr = await _dBContext.QueryAsync<string>("select dpvalue from `EquipmentDesignParam` where eqid=@eqid and dpname=@dpname", new { eqid = equipId, dpname = "HKBaseInfo" });

            if (configsStr == null || configsStr.Count() == 0)
            {
                return null;
            }
            var configStr = configsStr.FirstOrDefault();

            if (string.IsNullOrEmpty(configStr))
            {
                return null;
            }

            var config = JsonConvert.DeserializeObject<VideoBaseConfig>(configStr);
            return config;
        }
        public async Task TurnRoundUp(int equipId, int direction, int speed = 0)
        {
            try
            {
                var result = HaiKangFactory.TurnRoundUp(equipId.ToString(), Context.ConnectionId, direction);
                if (!result.Success)
                {
                    var config = await GetEquipConfig(equipId);
                    if (config == null)
                    {
                        var client = Clients.Client(Context.ConnectionId);
                        await client.SendAsync("ReturnVideo", new { Success = false, Msg = "用户基本信息不存在！", Method = "TurnRoundUp" });
                        return;
                    }

                    HaiKangFactory.Login(equipId.ToString(), config, Context.ConnectionId);

                    HaiKangFactory.TurnRoundUp(equipId.ToString(), Context.ConnectionId, direction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("TurnRoundUp", ex.Message);
            }

            await Task.CompletedTask;
        }
        public async Task TurnRoundDown(long equipId, int direction, int speed = 0)
        {
            try
            {
                var result = HaiKangFactory.TurnRoundDown(equipId.ToString(), Context.ConnectionId, direction);
                if (!result.Success)
                {
                    var config = await GetEquipConfig(equipId);
                    if (config == null)
                    {
                        var client = Clients.Client(Context.ConnectionId);
                        await client.SendAsync("ReturnVideo", new { Success = false, Msg = "用户基本信息不存在！", Method = "TurnRoundUp" });
                        return;
                    }


                    HaiKangFactory.Login(equipId.ToString(), config, Context.ConnectionId);

                    HaiKangFactory.TurnRoundUp(equipId.ToString(), Context.ConnectionId, direction);
                }
                if (!result.Success)
                {
                    var config = await GetEquipConfig(equipId);
                    if (config == null)
                    {
                        var client = Clients.Client(Context.ConnectionId);
                        await client.SendAsync("ReturnVideo", new { Success = false, Msg = "用户基本信息不存在！", Method = "TurnRoundUp" });
                        return;
                    }

                    HaiKangFactory.Login(equipId.ToString(), config, Context.ConnectionId);

                    HaiKangFactory.TurnRoundDown(equipId.ToString(), Context.ConnectionId, direction);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("TurnRoundDown", ex.Message);
            }

            await Task.CompletedTask;
        }
        public async Task CaptureJPEGPictureBase64(long equipId)
        {
            try
            {
                string base64Str = HaiKangFactory.CaptureJPEGPictureBase64(equipId.ToString(), Context.ConnectionId);
                await Clients.Client(Context.ConnectionId).SendAsync("ReturnVideo", new { Success = true, Data = base64Str, Method = "CaptureJPEGPictureBase64" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("CaptureJPEGPictureBase64", ex.Message);
            }
        }
        public async Task SetAlarm(long equipId)
        {
            try
            {
                var data = HaiKangFactory.SetAlarm(equipId.ToString(), Context.ConnectionId);
                data.Method = "SetAlarm";
                await Clients.Client(Context.ConnectionId).SendAsync("ReturnVideo", data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("SetAlarm", ex.Message);
            }
        }
        public async Task CloseAlarm(long equipId)
        {
            try
            {
                var config = await GetEquipConfig(equipId);
                if (config == null)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ReturnVideo", new { Success = false, Msg = "用户基本信息不存在！", Method = "CloseAlarm" });
                    return;
                }
                HaiKangFactory.Login(equipId.ToString(), config, Context.ConnectionId);
                var data = HaiKangFactory.CloseAlarm(equipId.ToString(), Context.ConnectionId);
                data.Method = "CloseAlarm";
                await Clients.Client(Context.ConnectionId).SendAsync("ReturnVideo", data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("CloseAlarm", ex.Message);
            }
        }
        public async Task StopVideo(long equipId)
        {
            try
            {
                HaiKangFactory.StopVideo(equipId.ToString(), Context.ConnectionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("StopVideo", ex.Message);
            }
            await Task.CompletedTask;
        }
        public void InsertAlarm(string ip, string alarmMsg, string type, int faultType, string equipIdStr)
        {
            try
            {
                int _fauType = 45;
                #region 枚举转换
                if (type == "COMM_ALARM" || type == "COMM_ALARM_V30")
                {
                    switch (faultType)
                    {
                        case 0:
                            _fauType = 1008;
                            break;
                        case 1:
                            _fauType = 1009;
                            break;
                        case 2:
                            _fauType = 1010;
                            break;
                        case 3:
                            _fauType = 1011;
                            break;
                        case 4:
                            _fauType = 1012;
                            break;
                        case 5:
                            _fauType = 1013;
                            break;
                        case 6:
                            _fauType = 1014;
                            break;
                        case 7:
                            _fauType = 1015;
                            break;
                        case 8:
                            _fauType = 1016;
                            break;
                        case 9:
                            _fauType = 1017;
                            break;
                        case 10:
                            _fauType = 1019;
                            break;
                        case 11:
                            _fauType = 1020;
                            break;
                        case 12:
                            _fauType = 1021;
                            break;
                        case 13:
                            _fauType = 1022;
                            break;
                        case 15:
                            _fauType = 1023;
                            break;
                    }
                }
                else if (type == "COMM_ALARM_RULE")
                {
                    switch (faultType)
                    {
                        case 1:
                            _fauType = 1024;
                            break;
                        case 2:
                            _fauType = 1025;
                            break;
                        case 3:
                            _fauType = 1026;
                            break;
                        case 4:
                            _fauType = 1027;
                            break;
                        case 5:
                            _fauType = 1028;
                            break;
                    }
                }
                else if (type == "COMM_UPLOAD_PLATE_RESULT" || type == "COMM_ITS_PLATE_RESULT")
                {
                    _fauType = 1029;
                }
                else if (type == "COMM_ALARM_TPS_REAL_TIME" || type == "TPS统计过车数据")
                {
                    if (faultType == 0)
                        _fauType = 1030;
                    else if (faultType == 1)
                        _fauType = 1031;
                }
                else if (type == "COMM_ALARM_PDC")
                {
                    _fauType = 1032;
                }
                else if (type == "COMM_ITS_PARK_VEHICLE")
                {
                    _fauType = 1033;
                }
                else if (type == "COMM_DIAGNOSIS_UPLOAD")
                {
                    _fauType = 1034;
                }
                else if (type == "COMM_UPLOAD_FACESNAP_RESULT" || type == "COMM_SNAP_MATCH_ALARM")
                {
                    if (faultType == 0)
                        _fauType = 1035;
                    else if (faultType == 1)
                        _fauType = 1036;
                    else if (faultType == 2)
                        _fauType = 1037;
                }
                else if (type == "COMM_ALARMHOST_CID_ALARM")
                {
                    _fauType = 1038;
                }
                else if (type == "COMM_UPLOAD_VIDEO_INTERCOM_EVENT")
                {
                    _fauType = 1039;
                }
                else if (type == "COMM_ALARM_ACS")
                {
                    _fauType = 1040;
                }
                else if (type == "COMM_ID_INFO_ALARM")
                {
                    _fauType = 1041;
                }
                else if (type == "COMM_UPLOAD_AIOP_VIDEO" || type == "COMM_UPLOAD_AIOP_PICTURE")
                {
                    if (faultType == 0)
                        _fauType = 1042;
                    else if (faultType == 1)
                        _fauType = 1043;
                }
                else if (type == "COMM_ISAPI_ALARM")
                {
                    _fauType = 1044;
                }


                #endregion
                //Console.WriteLine($"设备Id：{equipIdStr};IP地址：{ip};报警信息：{alarmMsg};方法：{type};枚举值：{faultType}");
                int equipIdInt = 0;
                int.TryParse(equipIdStr, out equipIdInt);

                var data = new
                {
                    Id = UidGenerator.Uid(),
                    Eqid = equipIdInt,
                    Faucodeid = 0,
                    Status = 3,
                    Faudesc = $"设备Id：{equipIdStr};IP地址：{ip};报警信息：{alarmMsg};方法：{type};枚举值：{faultType}",
                    Fautype = _fauType
                };
                _dBContextAir.Execute(@"insert into `WeiCloudAirDB`.`Tb_Fault_History` 
	(`id`,
	`eqid`,  
	`faucodeid`, 
	`status`, 
	`faudesc`, 
	`energytype`
	)
	values
	(@id,
	@eqid,  
	@faucodeid, 
	@status, 
	@faudesc, 
	@energytype
	);", data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        public async Task AlarmStartListen(int equipId)
        {
            try
            {
                string equipIdStr = equipId.ToString();
                var data = HaiKangFactory.AlarmStartListen(equipId.ToString(), Context.ConnectionId, (ip, alarmMsg, type, faultType) =>
                {
                    try
                    {
                        InsertAlarm(ip, alarmMsg, type, faultType, equipIdStr);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        _logger.LogError("AlarmStartListen-InsertAlarm", ex.Message);
                    }
                });
                data.Method = "AlarmStartListen";
                await Clients.Client(Context.ConnectionId).SendAsync("ReturnVideo", data);
                if (data.Success)
                {
                    var config = await GetEquipConfig(equipId);
                    if (config != null)
                    {
                        config.IsSetUpAlarm = 1;
                        await _dBContext.ExecuteAsync("update `EquipmentDesignParam` set dpvalue=@dpvalue where eqid=@eqid and dpname=@dpname", new { eqid = equipId, dpname = "HKBaseInfo", dpvalue = JsonConvert.SerializeObject(config) });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("AlarmStartListen", ex.Message);
            }
        }
        public async Task AlarmStopListen(int equipId)
        {
            try
            {
                var data = HaiKangFactory.AlarmStopListen(equipId.ToString(), Context.ConnectionId);
                data.Method = "AlarmStopListen";
                await Clients.Client(Context.ConnectionId).SendAsync("ReturnVideo", data);
                if (data.Success)
                {
                    var config = await GetEquipConfig(equipId);
                    if (config != null)
                    {
                        config.IsSetUpAlarm = 0;
                        await _dBContext.ExecuteAsync("update `EquipmentDesignParam` set dpvalue=@dpvalue where eqid=@eqid and dpname=@dpname", new { eqid = equipId, dpname = "HKBaseInfo", dpvalue = JsonConvert.SerializeObject(config) });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("AlarmStopListen", ex.Message);
            }
        }

        public async Task Zoom(int equipId, int isStart, int isIn)
        {
            try
            {

                HaiKangFactory.Zoom(equipId.ToString(), Context.ConnectionId, isStart, isIn);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("Zoom", ex.Message);
            }
        }
        public async Task Cruise(int equipId, int isStart)
        {
            try
            {
                var result = HaiKangFactory.Cruise(equipId.ToString(), Context.ConnectionId, isStart);
                result.Method = "Cruise";
                if (result.Success)
                {
                    var config = await GetEquipConfig(equipId);
                    if (config != null)
                    {
                        config.IsCruise = isStart;
                        await _dBContext.ExecuteAsync("update `EquipmentDesignParam` set dpvalue=@dpvalue where eqid=@eqid and dpname=@dpname", new { eqid = equipId, dpname = "HKBaseInfo", dpvalue = JsonConvert.SerializeObject(config) });
                    }
                }
                await Clients.Client(Context.ConnectionId).SendAsync("ReturnVideo", result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _logger.LogError("Cruise", ex.Message);
            }
        }
    }
}
