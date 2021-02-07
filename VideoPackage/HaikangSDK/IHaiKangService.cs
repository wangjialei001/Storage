using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

namespace HaikangSDK
{
    public abstract class IHaiKangService:IDisposable
    {
        public string currentPath = string.Empty;
        public Int32 m_lUserID = -1;
        public abstract void Logout();
        public abstract void Show();
        public abstract void Login(string DVRIPAddress, string DVRPort, string DVRUserName, string DVRPassword,int ChannelNum);
        public abstract void StopShow();

        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Release managed resources
                }

                // Release unmanaged resources

                m_disposed = true;
            }
        }
        ~IHaiKangService()
        {
            Dispose(false);
        }
        public IHaiKangService()
        {
            try
            {
                currentPath = Directory.GetCurrentDirectory();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private bool m_disposed;

        //public delegate void ReturnBufDelegate(byte[] bytes);
        //public ReturnBufDelegate ReturnBuf;

        public delegate void ReturnBufBase64Delegate(string imageBase64);
        public ReturnBufBase64Delegate ReturnBufBase64;


        public abstract void CleanUp();

        public abstract void TurnRoundUp(int speed, int direction);
        public abstract void TurnRoundDown(int speed, int direction);

        public abstract string CaptureJPEGPictureBase64();

        public int TurnRoundSpeed = 4;

        public int m_lAlarmHandle = -1;

        public abstract ReturnVideo SetAlarm();
        public abstract ReturnVideo CloseAlarm();
        public int iListenHandle = -1;
        public abstract ReturnVideo AlarmStartListen();
        public abstract ReturnVideo AlarmStopListen();
        public string localIP { get; set; }
        public ushort localPort = 7200;
        public void GetLocalIP()
        {
            byte[] strIP = new byte[16 * 16];
            uint dwValidNum = 0;
            Boolean bEnableBind = false;

            //获取本地PC网卡IP信息

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                if (CHCNetSDK_Win64.NET_DVR_GetLocalIP(strIP, ref dwValidNum, ref bEnableBind))
                {
                    if (dwValidNum > 0)
                    {
                        //取第一张网卡的IP地址为默认监听端口
                        localIP = System.Text.Encoding.UTF8.GetString(strIP, 0, 16);
                    }
                }
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                if (CHCNetSDK_Linux.NET_DVR_GetLocalIP(strIP, ref dwValidNum, ref bEnableBind))
                {
                    if (dwValidNum > 0)
                    {
                        //取第一张网卡的IP地址为默认监听端口
                        localIP = System.Text.Encoding.UTF8.GetString(strIP, 0, 16);
                    }
                }
            }
        }

        
        public abstract void Zoom(int isStart, int isIn);
        public abstract ReturnVideo Cruise(int isStart,int byCruiseRoute);

        public delegate void AlarmDelegate(string ip, string alarmMsg, string type, int alarmType);
        public AlarmDelegate ReturnAlarm;

        public void SaveAlarmPic(string fileName, int iLen, IntPtr pBuffer1)
        {
            string path = string.Empty;
            try
            {
                path = Path.Combine(currentPath, "AlarmPic");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path = Path.Combine(path, fileName);
                FileStream fs = new FileStream(path, FileMode.Create);

                byte[] by = new byte[iLen];
                Marshal.Copy(pBuffer1, by, 0, iLen);
                fs.Write(by, 0, iLen);
                fs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveAlarmPic-SaveAlarmPic-" + path + ";" + ex.Message);
            }
        }
    }
}
