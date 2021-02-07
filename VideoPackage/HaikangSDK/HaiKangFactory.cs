using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using static HaikangSDK.IHaiKangService;

namespace HaikangSDK
{
    public class HaiKangFactory
    {
        private static Dictionary<string, VideoBase> Dic = new Dictionary<string, VideoBase>();

        private static Dictionary<string, string> DicCurrentIdMapKey = new Dictionary<string, string>();

        private static Dictionary<string, ReturnBufBase64Delegate> DicConnIdMapCallback = new Dictionary<string, ReturnBufBase64Delegate>();
        private static Dictionary<string, AlarmDelegate> DicConnIdMapAlarmCallback = new Dictionary<string, AlarmDelegate>();

        public static void Login(string key, VideoBaseConfig config, string currentId)
        {
            IHaiKangService haiKangService = null;
            var data = Init(key, config, currentId);
            if (data.Item1 != null && data.Item1.m_lUserID > -1)
            {
                return;
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Console.WriteLine("当前是window环境");
                haiKangService = new HaiKangService_Win64();
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                Console.WriteLine("当前是linux环境");
                haiKangService = new HaiKangService_Linux();
            }
            Dic.Add(key, new VideoBase
            {
                Service = haiKangService,
                Config = config
            });
            var videoConfig = config.VideoConfig;
            haiKangService.Login(videoConfig.IP, videoConfig.Port, videoConfig.UserName, videoConfig.Pwd, videoConfig.ChannelNum);

        }
        public static void GetVideo(string key, string currentId, ReturnBufBase64Delegate callback)
        {
            var data = Init(key, null, currentId);
            IHaiKangService haiKangService = data.Item1;

            var videoConfig = data.Item2.VideoConfig;
            if (!DicConnIdMapCallback.ContainsKey(currentId))
            {
                DicConnIdMapCallback.Add(currentId, callback);
                haiKangService.ReturnBufBase64 += callback;
            }
            haiKangService.Show();
        }
        public static void CustomerClose(string currentId)
        {
            if (DicCurrentIdMapKey.ContainsKey(currentId))
            {
                var key = DicCurrentIdMapKey[currentId];
                StopVideo(key, currentId);
                DicCurrentIdMapKey.Remove(currentId);
            }
        }
        public static void ClearUp(string key, IHaiKangService haiKangService = null)
        {
            if (haiKangService == null && !string.IsNullOrEmpty(key))
            {
                var data = Init(key);
                haiKangService = data.Item1;
            }

            if (haiKangService == null)
                return;

            if ((haiKangService.ReturnBufBase64 == null || haiKangService.ReturnBufBase64.GetInvocationList().Length == 0))
            {
                haiKangService.StopShow();
                if ((haiKangService.ReturnAlarm == null || haiKangService.ReturnAlarm.GetInvocationList().Length == 0))
                {
                    haiKangService.Logout();
                    haiKangService.CleanUp();
                    haiKangService.Dispose();
                    Dic.Remove(key);
                    Console.WriteLine("释放IHaiKangService对象");
                }
            }
        }
        public static void StopVideo(string key, string currentId)
        {
            var data = Init(key, null, currentId);
            IHaiKangService haiKangService = data.Item1;
            if (haiKangService != null)
            {
                if (DicConnIdMapCallback.ContainsKey(currentId))
                {
                    var callback = DicConnIdMapCallback[currentId];
                    if (haiKangService.ReturnBufBase64 != null)
                        haiKangService.ReturnBufBase64 -= callback;
                    DicConnIdMapCallback.Remove(currentId);
                }
                ClearUp(key);
            }
        }

        public static ReturnVideo TurnRoundUp(string key, string currentId, int direction = 0, int turnSpeed = 0)
        {
            var data = Init(key, null, currentId);

            IHaiKangService haiKangService = data.Item1;
            VideoBaseConfig config = data.Item2;
            if (haiKangService == null || haiKangService.m_lUserID < 0)
            {
                return new ReturnVideo
                {
                    Success = false
                };
            }
            haiKangService.TurnRoundUp(turnSpeed > 0 ? turnSpeed : config.TurnSpeed, direction);
            return new ReturnVideo { Success = true };
        }
        public static ReturnVideo TurnRoundDown(string key, string currentId, int direction = 0, int turnSpeed = 0)
        {
            var data = Init(key, null, currentId);

            IHaiKangService haiKangService = data.Item1;
            VideoBaseConfig config = data.Item2;
            if (haiKangService == null || haiKangService.m_lUserID < 0)
            {
                return new ReturnVideo { Success = false };
            }
            haiKangService.TurnRoundDown(turnSpeed > 0 ? turnSpeed : config.TurnSpeed, direction);
            return new ReturnVideo { Success = true };
        }
        public static string CaptureJPEGPictureBase64(string key, string currentId)
        {
            var data = Init(key, null, currentId);
            IHaiKangService haiKangService = data.Item1;
            return haiKangService.CaptureJPEGPictureBase64();
        }
        public static ReturnVideo SetAlarm(string key, string currentId)
        {
            var data = Init(key, null, currentId);
            IHaiKangService haiKangService = data.Item1;
            return haiKangService.SetAlarm();
        }
        public static ReturnVideo CloseAlarm(string key, string currentId)
        {
            var data = Init(key, null, currentId);
            IHaiKangService haiKangService = data.Item1;
            return haiKangService.CloseAlarm();
        }

        public static ReturnVideo AlarmStartListen(string key, string currentId, AlarmDelegate callback)
        {
            var data = Init(key, null, currentId);
            IHaiKangService haiKangService = data.Item1;
            if (!DicConnIdMapAlarmCallback.ContainsKey(currentId) && haiKangService.ReturnAlarm == null)
            {
                Console.WriteLine(key + "：已添加监控");
                DicConnIdMapAlarmCallback.Add(currentId, callback);
                haiKangService.ReturnAlarm = callback;
            }
            return haiKangService.AlarmStartListen();
        }
        public static ReturnVideo AlarmStopListen(string key, string currentId)
        {
            var data = Init(key, null, currentId);
            IHaiKangService haiKangService = data.Item1;
            if (DicConnIdMapAlarmCallback.ContainsKey(currentId))
            {
                var callback = DicConnIdMapAlarmCallback[currentId];
                DicConnIdMapAlarmCallback.Remove(currentId);
            }
            haiKangService.ReturnAlarm = null;
            Console.WriteLine(key + "：取消监控");
            var returnVideo = haiKangService.AlarmStopListen();

            ClearUp(string.Empty, haiKangService);
            return returnVideo;
        }


        private static Tuple<IHaiKangService, VideoBaseConfig> Init(string key, VideoBaseConfig _config = null, string currentId = "")
        {
            IHaiKangService haiKangService = null;
            VideoBaseConfig config = null;

            if (Dic.ContainsKey(key))
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    Console.WriteLine("当前是window环境");
                    haiKangService = HaiKangFactory.Dic[key].Service as HaiKangService_Win64;
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    Console.WriteLine("当前是linux环境");
                    haiKangService = HaiKangFactory.Dic[key].Service as HaiKangService_Linux;
                }
                if (_config != null)
                    HaiKangFactory.Dic[key].Config = _config;
                config = HaiKangFactory.Dic[key].Config;
            }
            if (!string.IsNullOrEmpty(currentId) && !DicCurrentIdMapKey.ContainsKey(currentId))
            {
                DicCurrentIdMapKey.Add(currentId, key);
            }
            return new Tuple<IHaiKangService, VideoBaseConfig>(haiKangService, config);
        }

        public static void Zoom(string key, string currentId, int isStart, int isIn)
        {
            try
            {
                var data = Init(key, null, currentId);
                IHaiKangService haiKangService = data.Item1;
                haiKangService.Zoom(isStart, isIn);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public static ReturnVideo Cruise(string key, string currentId, int isStart)
        {
            try
            {
                var data = Init(key, null, currentId);
                IHaiKangService haiKangService = data.Item1;
                var config = data.Item2;
                if (config == null || config.CruiseRouteId == 0)
                {
                    return new ReturnVideo { Success = false, Msg = "不存在巡航路径！" };
                }
                return haiKangService.Cruise(isStart, config.CruiseRouteId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return new ReturnVideo { Success = false };
        }
    }
    public class VideoConfig
    {
        public string IP { get; set; }
        public string Port { get; set; }
        public string UserName { get; set; }
        public string Pwd { get; set; }
        public int ChannelNum { get; set; }
    }
    public class VideoBaseConfig
    {
        public VideoConfig VideoConfig { get; set; }
        public int TurnSpeed { get; set; }
        /// <summary>
        /// 是否布放
        /// </summary>
        public int IsSetUpAlarm { get; set; }

        public int CruiseRouteId { get; set; }
        public int IsCruise { get; set; }
        public int EquipId { get; set; }
    }
    public class VideoBase
    {
        public VideoBaseConfig Config { get; set; }
        public IHaiKangService Service { get; set; }
    }
    public class ControlInfo
    {
        /// <summary>
        /// 1-转动摄像头；
        /// </summary>
        public int Type { get; set; }
        public object Data { get; set; }
    }
    public class ControlTurn
    {
        public int Direction { get; set; }
        public int Speed { get; set; }
    }
    public class ReturnVideo
    {
        public bool Success { get; set; }
        public object Data { get; set; }
        public string Method { get; set; }
        public string Msg { get; set; }
    }
}
