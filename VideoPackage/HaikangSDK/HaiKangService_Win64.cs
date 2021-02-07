using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HaikangSDK
{
    public class HaiKangService_Win64 : IHaiKangService
    {
        private bool m_bInitSDK = false;
        private bool m_bRecord = false;
        private uint iLastErr = 0;

        private Int32 m_lRealHandle = -1;
        private string str1;
        private string str2;
        private Int32 i = 0;
        private Int32 m_lTree = 0;
        private string str;
        private long iSelIndex = 0;
        private uint dwAChanTotalNum = 0;
        private uint dwDChanTotalNum = 0;
        private Int32 m_lPort = -1;
        private IntPtr m_ptrRealHandle;
        private int[] iIPDevID = new int[96];
        private int[] iChannelNum = new int[96];

        private CHCNetSDK_Win64.REALDATACALLBACK RealData = null;
        public CHCNetSDK_Win64.NET_DVR_DEVICEINFO_V30 DeviceInfo;
        public CHCNetSDK_Win64.NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;
        public CHCNetSDK_Win64.NET_DVR_STREAM_MODE m_struStreamMode;
        public CHCNetSDK_Win64.NET_DVR_IPCHANINFO m_struChanInfo;
        public CHCNetSDK_Win64.NET_DVR_PU_STREAM_URL m_struStreamURL;
        public CHCNetSDK_Win64.NET_DVR_IPCHANINFO_V40 m_struChanInfoV40;
        private PlayCtrl_Win64.DECCBFUN m_fDisplayFun = null;
        public delegate void MyDebugInfo(string str);

        //MonitorService monitorService = new MonitorService();
        public HaiKangService_Win64()
        {
            m_bInitSDK = CHCNetSDK_Win64.NET_DVR_Init();
            if (m_bInitSDK == false)
            {
                Console.WriteLine("NET_DVR_Init error!");
                return;
            }
            else
            {
                //保存SDK日志 To save the SDK log
                CHCNetSDK_Win64.NET_DVR_SetLogToFile(1, "C:\\SdkLog1\\", true);


                for (int i = 0; i < 64; i++)
                {
                    iIPDevID[i] = -1;
                    iChannelNum[i] = -1;
                }
                GetLocalIP();
            }
        }
        public override void Logout()
        {
            if (m_lUserID >= 0)
            {
                //注销登录 Logout the device
                if (m_lRealHandle >= 0)
                {
                    DebugInfo("Please stop live view firstly"); //登出前先停止预览 Stop live view before logout
                    return;
                }

                if (!CHCNetSDK_Win64.NET_DVR_Logout(m_lUserID))
                {
                    iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                    str = "NET_DVR_Logout failed, error code= " + iLastErr;
                    DebugInfo(str);
                    return;
                }
                DebugInfo("NET_DVR_Logout succ!");
                //listViewIPChannel.Items.Clear();//清空通道列表 Clean up the channel list
                m_lUserID = -1;
                //btnLogin.Text = "Login";
            }
        }
        public override void Login(string DVRIPAddress, string DVRPort, string DVRUserName, string DVRPassword,int ChannelNum)
        {
            if (m_lUserID < 0)
            {
                Int16 DVRPortNumber = Int16.Parse(DVRPort);


                //登录设备 Login the device
                m_lUserID = CHCNetSDK_Win64.NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo);


                if (m_lUserID < 0)
                {
                    iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                    str = "NET_DVR_Login_V30 failed, error code= " + iLastErr; //登录失败，输出错误号 Failed to login and output the error code
                    DebugInfo(str);
                    return;
                }
                else
                {
                    iSelIndex = ChannelNum;
                    //登录成功
                    DebugInfo("NET_DVR_Login_V30 succ!");
                    //btnLogin.Text = "Logout";

                    dwAChanTotalNum = (uint)DeviceInfo.byChanNum;
                    dwDChanTotalNum = (uint)DeviceInfo.byIPChanNum + 256 * (uint)DeviceInfo.byHighDChanNum;
                    if (dwDChanTotalNum > 0)
                    {
                        InfoIPChannel();
                    }
                    else
                    {
                        for (i = 0; i < dwAChanTotalNum; i++)
                        {
                            //ListAnalogChannel(i + 1, 1);
                            iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                        }

                        //comboBoxView.SelectedItem = 1;
                        // Console.WriteLine("This device has no IP channel!");
                    }
                }

            }

            return;
        }
        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            CaptureJPEGPicture();
        }
        public override string CaptureJPEGPictureBase64()
        {
            CHCNetSDK_Win64.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK_Win64.NET_DVR_JPEGPARA();
            lpJpegPara.wPicQuality = 0; //图像质量 Image quality
            lpJpegPara.wPicSize = 0xff; //抓图分辨率 Picture size: 0xff-Auto(使用当前码流分辨率) 

            //JEPG抓图，数据保存在缓冲区中 Capture a JPEG picture and save in the buffer
            uint iBuffSize = 400000; //缓冲区大小需要不小于一张图片数据的大小 The buffer size should not be less than the picture size
            byte[] byJpegPicBuffer = new byte[iBuffSize];
            uint dwSizeReturned = 0;

            if (!CHCNetSDK_Win64.NET_DVR_CaptureJPEGPicture_NEW(m_lUserID, iChannelNum[(int)iSelIndex], ref lpJpegPara, byJpegPicBuffer, iBuffSize, ref dwSizeReturned))
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                str = "NET_DVR_CaptureJPEGPicture_NEW failed, error code= " + iLastErr;
                //DebugInfo(str);
                Console.WriteLine(str);
                return str;
            }
            else
            {
                //将缓冲区里的JPEG图片数据写入文件 save the data into a file
                //string str = Guid.NewGuid().ToString() + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)dwSizeReturned;
                //fs.Write(byJpegPicBuffer, 0, iLen);
                //fs.Close();

                str = "NET_DVR_CaptureJPEGPicture_NEW succ and save the data in buffer to 'buffertest.jpg'.";
                //DebugInfo(str);
                string data = Convert.ToBase64String(byJpegPicBuffer);
                return data;
            }
        }

        public void CaptureJPEGPicture()
        {
            CHCNetSDK_Win64.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK_Win64.NET_DVR_JPEGPARA();
            //lpJpegPara.wPicQuality = 0; //图像质量 Image quality
            lpJpegPara.wPicQuality = 2; //图像质量 Image quality
            //lpJpegPara.wPicSize = 0xff; //抓图分辨率 Picture size: 0xff-Auto(使用当前码流分辨率) 
            lpJpegPara.wPicSize = 2; //抓图分辨率 Picture size: 0xff-Auto(使用当前码流分辨率) 

            //JEPG抓图，数据保存在缓冲区中 Capture a JPEG picture and save in the buffer
            //uint iBuffSize = 400000; //缓冲区大小需要不小于一张图片数据的大小 The buffer size should not be less than the picture size
            uint iBuffSize = 800000; //缓冲区大小需要不小于一张图片数据的大小 The buffer size should not be less than the picture size
            byte[] byJpegPicBuffer = new byte[iBuffSize];
            uint dwSizeReturned = 0;

            if (!CHCNetSDK_Win64.NET_DVR_CaptureJPEGPicture_NEW(m_lUserID, iChannelNum[(int)iSelIndex], ref lpJpegPara, byJpegPicBuffer, iBuffSize, ref dwSizeReturned))
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                str = "NET_DVR_CaptureJPEGPicture_NEW failed, error code= " + iLastErr;
                //DebugInfo(str);
                return;
            }
            else
            {
                //将缓冲区里的JPEG图片数据写入文件 save the data into a file
                //string str = Guid.NewGuid().ToString() + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)dwSizeReturned;
                //fs.Write(byJpegPicBuffer, 0, iLen);
                //fs.Close();

                str = "NET_DVR_CaptureJPEGPicture_NEW succ and save the data in buffer to 'buffertest.jpg'.";
                //DebugInfo(str);
                string data = Convert.ToBase64String(byJpegPicBuffer);
                if (ReturnBufBase64 != null)
                    ReturnBufBase64(data);
            }
        }

        //解码回调函数
        private void DecCallbackFUN(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl_Win64.FRAME_INFO pFrameInfo, int nReserved1, int nReserved2)
        {
            // 将pBuf解码后视频输入写入文件中（解码后YUV数据量极大，尤其是高清码流，不建议在回调函数中处理）
            if (pFrameInfo.nType == 3) //#define T_YV12	3
            {
                //FileStream fs = null;
                //BinaryWriter bw = null;
                //try
                //{
                //    fs = new FileStream("DecodedVideo.yuv", FileMode.Append);
                //    bw = new BinaryWriter(fs);
                //    byte[] byteBuf = new byte[nSize];
                //    Marshal.Copy(pBuf, byteBuf, 0, nSize);
                //    bw.Write(byteBuf);
                //    bw.Flush();
                //}
                //catch (System.Exception ex)
                //{
                //    Console.WriteLine(ex.ToString());
                //}
                //finally
                //{
                //    bw.Close();
                //    fs.Close();
                //}
            }
            //Console.WriteLine("DecCallbackFUN回调："+ pFrameInfo.nType);
        }
        public override void StopShow()
        {
            if (m_lUserID < 0)
            {
                Console.WriteLine("Please login the device firstly!");
                return;
            }

            if (m_bRecord)
            {
                Console.WriteLine("Please stop recording firstly!");
                return;
            }

            if (m_lRealHandle >= 0)
            {
                //停止预览 Stop live view 
                if (!CHCNetSDK_Win64.NET_DVR_StopRealPlay(m_lRealHandle))
                {
                    iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                    str = "NET_DVR_StopRealPlay failed, error code= " + iLastErr;
                    DebugInfo(str);
                    return;
                }

                //if ((comboBoxView.SelectedIndex == 1) && (m_lPort >= 0))
                if (m_lPort >= 0)
                {
                    if (!PlayCtrl_Win64.PlayM4_Stop(m_lPort))
                    {
                        iLastErr = PlayCtrl_Win64.PlayM4_GetLastError(m_lPort);
                        str = "PlayM4_Stop failed, error code= " + iLastErr;
                        DebugInfo(str);
                    }
                    if (!PlayCtrl_Win64.PlayM4_CloseStream(m_lPort))
                    {
                        iLastErr = PlayCtrl_Win64.PlayM4_GetLastError(m_lPort);
                        str = "PlayM4_CloseStream failed, error code= " + iLastErr;
                        DebugInfo(str);
                    }
                    if (!PlayCtrl_Win64.PlayM4_FreePort(m_lPort))
                    {
                        iLastErr = PlayCtrl_Win64.PlayM4_GetLastError(m_lPort);
                        str = "PlayM4_FreePort failed, error code= " + iLastErr;
                        DebugInfo(str);
                    }
                    m_lPort = -1;
                }

                DebugInfo("NET_DVR_StopRealPlay succ!");
                m_lRealHandle = -1;
                //btnPreview.Text = "Live View";
                //RealPlayWnd.Invalidate();//刷新窗口 refresh the window
            }
        }
        public override void Show()
        {
            if (m_lUserID < 0)
            {
                Console.WriteLine("Please login the device firstly!");
                return;
            }

            if (m_bRecord)
            {
                Console.WriteLine("Please stop recording firstly!");
                return;
            }

            if (m_lRealHandle < 0)
            {
                CHCNetSDK_Win64.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK_Win64.NET_DVR_PREVIEWINFO();
                //lpPreviewInfo.hPlayWnd = RealPlayWnd.Handle;//预览窗口 live view window
                lpPreviewInfo.lChannel = iChannelNum[(int)iSelIndex];//预览的设备通道 the device channel number
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 15; //播放库显示缓冲区最大帧数

                IntPtr pUser = IntPtr.Zero;//用户数据 user data 

                // (comboBoxView.SelectedIndex == 0)
                //{
                //打开预览 Start live view 
                //    m_lRealHandle = CHCNetSDK_Win64.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, null/*RealData*/, pUser);
                //}
                //else
                {
                    //lpPreviewInfo.hPlayWnd = IntPtr.Zero;//预览窗口 live view window
                    //m_ptrRealHandle = RealPlayWnd.Handle;
                    RealData = new CHCNetSDK_Win64.REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数 real-time stream callback function 
                    m_lRealHandle = CHCNetSDK_Win64.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, RealData, pUser);
                }

                if (m_lRealHandle < 0)
                {
                    iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                    str = "NET_DVR_RealPlay_V40 failed, error code= " + iLastErr; //预览失败，输出错误号 failed to start live view, and output the error code.
                    DebugInfo(str);
                    return;
                }
                else
                {
                    //预览成功
                    DebugInfo("NET_DVR_RealPlay_V40 succ!");
                    //btnPreview.Text = "Stop View";
                }
            }

            return;
        }

        public void DebugInfo(string str)
        {
            if (str.Length > 0)
            {
                str += "\n";
                //TextBoxInfo.AppendText(str);
                Console.WriteLine(str);
            }
        }
        public void InfoIPChannel()
        {
            uint dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40);

            IntPtr ptrIpParaCfgV40 = Marshal.AllocHGlobal((Int32)dwSize);
            Marshal.StructureToPtr(m_struIpParaCfgV40, ptrIpParaCfgV40, false);

            uint dwReturn = 0;
            int iGroupNo = 0;  //该Demo仅获取第一组64个通道，如果设备IP通道大于64路，需要按组号0~i多次调用NET_DVR_GET_IPPARACFG_V40获取

            if (!CHCNetSDK_Win64.NET_DVR_GetDVRConfig(m_lUserID, CHCNetSDK_Win64.NET_DVR_GET_IPPARACFG_V40, iGroupNo, ptrIpParaCfgV40, dwSize, ref dwReturn))
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                str = "NET_DVR_GET_IPPARACFG_V40 failed, error code= " + iLastErr;
                //获取IP资源配置信息失败，输出错误号 Failed to get configuration of IP channels and output the error code
                DebugInfo(str);
            }
            else
            {
                DebugInfo("NET_DVR_GET_IPPARACFG_V40 succ!");

                m_struIpParaCfgV40 = (CHCNetSDK_Win64.NET_DVR_IPPARACFG_V40)Marshal.PtrToStructure(ptrIpParaCfgV40, typeof(CHCNetSDK_Win64.NET_DVR_IPPARACFG_V40));

                for (i = 0; i < dwAChanTotalNum; i++)
                {
                    ListAnalogChannel(i + 1, m_struIpParaCfgV40.byAnalogChanEnable[i]);
                    iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                }

                byte byStreamType = 0;
                uint iDChanNum = 64;

                if (dwDChanTotalNum < 64)
                {
                    iDChanNum = dwDChanTotalNum; //如果设备IP通道小于64路，按实际路数获取
                }

                for (i = 0; i < iDChanNum; i++)
                {
                    iChannelNum[i + dwAChanTotalNum] = i + (int)m_struIpParaCfgV40.dwStartDChan;
                    byStreamType = m_struIpParaCfgV40.struStreamMode[i].byGetStreamType;

                    dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40.struStreamMode[i].uGetStream);
                    switch (byStreamType)
                    {
                        //目前NVR仅支持直接从设备取流 NVR supports only the mode: get stream from device directly
                        case 0:
                            IntPtr ptrChanInfo = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfo, false);
                            m_struChanInfo = (CHCNetSDK_Win64.NET_DVR_IPCHANINFO)Marshal.PtrToStructure(ptrChanInfo, typeof(CHCNetSDK_Win64.NET_DVR_IPCHANINFO));

                            //列出IP通道 List the IP channel
                            ListIPChannel(i + 1, m_struChanInfo.byEnable, m_struChanInfo.byIPID);
                            iIPDevID[i] = m_struChanInfo.byIPID + m_struChanInfo.byIPIDHigh * 256 - iGroupNo * 64 - 1;

                            Marshal.FreeHGlobal(ptrChanInfo);
                            break;
                        case 4:
                            IntPtr ptrStreamURL = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrStreamURL, false);
                            m_struStreamURL = (CHCNetSDK_Win64.NET_DVR_PU_STREAM_URL)Marshal.PtrToStructure(ptrStreamURL, typeof(CHCNetSDK_Win64.NET_DVR_PU_STREAM_URL));

                            //列出IP通道 List the IP channel
                            ListIPChannel(i + 1, m_struStreamURL.byEnable, m_struStreamURL.wIPID);
                            iIPDevID[i] = m_struStreamURL.wIPID - iGroupNo * 64 - 1;

                            Marshal.FreeHGlobal(ptrStreamURL);
                            break;
                        case 6:
                            IntPtr ptrChanInfoV40 = Marshal.AllocHGlobal((Int32)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfoV40, false);
                            m_struChanInfoV40 = (CHCNetSDK_Win64.NET_DVR_IPCHANINFO_V40)Marshal.PtrToStructure(ptrChanInfoV40, typeof(CHCNetSDK_Win64.NET_DVR_IPCHANINFO_V40));

                            //列出IP通道 List the IP channel
                            ListIPChannel(i + 1, m_struChanInfoV40.byEnable, m_struChanInfoV40.wIPID);
                            iIPDevID[i] = m_struChanInfoV40.wIPID - iGroupNo * 64 - 1;

                            Marshal.FreeHGlobal(ptrChanInfoV40);
                            break;
                        default:
                            break;
                    }
                }
            }
            Marshal.FreeHGlobal(ptrIpParaCfgV40);

        }
        public void ListIPChannel(Int32 iChanNo, byte byOnline, int byIPID)
        {
            str1 = String.Format("IPCamera {0}", iChanNo);
            m_lTree++;

            if (byIPID == 0)
            {
                str2 = "X"; //通道空闲，没有添加前端设备 the channel is idle                  
            }
            else
            {
                if (byOnline == 0)
                {
                    str2 = "offline"; //通道不在线 the channel is off-line
                }
                else
                    str2 = "online"; //通道在线 The channel is on-line
            }

            //listViewIPChannel.Items.Add(new ListViewItem(new string[] { str1, str2 }));//将通道添加到列表中 add the channel to the list
        }
        public void ListAnalogChannel(Int32 iChanNo, byte byEnable)
        {
            str1 = String.Format("Camera {0}", iChanNo);
            m_lTree++;

            if (byEnable == 0)
            {
                str2 = "Disabled"; //通道已被禁用 This channel has been disabled               
            }
            else
            {
                str2 = "Enabled"; //通道处于启用状态 This channel has been enabled
            }

            //listViewIPChannel.Items.Add(new ListViewItem(new string[] { str1, str2 }));//将通道添加到列表中 add the channel to the list
        }

        public override void CleanUp()
        {
            CHCNetSDK_Win64.NET_DVR_Cleanup();
        }

        public override void TurnRoundDown(int speed, int direction)
        {
            if (speed <= 0)
                speed = TurnRoundSpeed;
            Console.WriteLine($"TurnRoundDown:{speed}、{direction}");
            if (m_lRealHandle >= 0)
            {
                if (direction == 0)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.TILT_UP, 0, (uint)speed);
                else if (direction == 1)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.TILT_DOWN, 0, (uint)speed);
                else if (direction == 2)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.PAN_LEFT, 0, (uint)speed);
                else if (direction == 3)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.PAN_RIGHT, 0, (uint)speed);
                else if (direction == 4)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.UP_LEFT, 0, (uint)speed);
                else if (direction == 5)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.UP_RIGHT, 0, (uint)speed);
                else if (direction == 6)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.DOWN_LEFT, 0, (uint)speed);
                else if (direction == 7)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.DOWN_RIGHT, 0, (uint)speed);
                else if (direction == 8)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.PAN_AUTO, 0, (uint)speed);
            }
            else
            {
                if (direction == 0)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.TILT_UP, 0, (uint)speed);
                else if (direction == 1)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.TILT_DOWN, 0, (uint)speed);
                else if (direction == 2)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.PAN_LEFT, 0, (uint)speed);
                else if (direction == 3)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.PAN_RIGHT, 0, (uint)speed);
                else if (direction == 4)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.UP_LEFT, 0, (uint)speed);
                else if (direction == 5)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.UP_RIGHT, 0, (uint)speed);
                else if (direction == 6)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.DOWN_LEFT, 0, (uint)speed);
                else if (direction == 7)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.DOWN_RIGHT, 0, (uint)speed);
                else if (direction == 8)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.PAN_AUTO, 0, (uint)speed);
            }
        }

        public override void TurnRoundUp(int speed, int direction)
        {
            if (speed <= 0)
                speed = TurnRoundSpeed;
            Console.WriteLine($"TurnRoundUp:{speed}、{direction}");
            if (m_lRealHandle >= 0)
            {
                if (direction == 0)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.TILT_UP, 1, (uint)speed);
                else if (direction == 1)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.TILT_DOWN, 1, (uint)speed);
                else if (direction == 2)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.PAN_LEFT, 1, (uint)speed);
                else if (direction == 3)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.PAN_RIGHT, 1, (uint)speed);
                else if (direction == 4)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.UP_LEFT, 1, (uint)speed);
                else if (direction == 5)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.UP_RIGHT, 1, (uint)speed);
                else if (direction == 6)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.DOWN_LEFT, 1, (uint)speed);
                else if (direction == 7)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.DOWN_RIGHT, 1, (uint)speed);
                else if (direction == 8)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed(m_lRealHandle, CHCNetSDK_Win64.PAN_AUTO, 1, (uint)speed);
            }
            else
            {
                if (direction == 0)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.TILT_UP, 1, (uint)speed);
                else if (direction == 1)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.TILT_DOWN, 1, (uint)speed);
                else if (direction == 2)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.PAN_LEFT, 1, (uint)speed);
                else if (direction == 3)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.PAN_RIGHT, 1, (uint)speed);
                else if (direction == 4)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.UP_LEFT, 1, (uint)speed);
                else if (direction == 5)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.UP_RIGHT, 1, (uint)speed);
                else if (direction == 6)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.DOWN_LEFT, 1, (uint)speed);
                else if (direction == 7)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.DOWN_RIGHT, 1, (uint)speed);
                else if (direction == 8)
                    CHCNetSDK_Win64.NET_DVR_PTZControlWithSpeed_Other(m_lUserID, iChannelNum[(int)iSelIndex], CHCNetSDK_Win64.PAN_AUTO, 1, (uint)speed);
            }
        }
        private CHCNetSDK_Win64.EXCEPYIONCALLBACK m_fExceptionCB = null;
        public void cbExceptionCB(uint dwType, int lUserID, int lHandle, IntPtr pUser)
        {
            //异常消息信息类型
            string stringAlarm = "异常消息回调，信息类型：0x" + Convert.ToString(dwType, 16) + ", lUserID:" + lUserID + ", lHandle:" + lHandle;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = lUserID;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallbackException(UpdateClientListException), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientListException(DateTime.Now.ToString(), lUserID, stringAlarm);
            //}
            Console.WriteLine(stringAlarm);
        }
        private CHCNetSDK_Win64.MSGCallBack_V31 m_falarmData_V31 = null;
        public bool MsgCallback_V31(int lCommand, ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            //通过lCommand来判断接收到的报警信息类型，不同的lCommand对应不同的pAlarmInfo内容
            AlarmMessageHandle(lCommand, ref pAlarmer, pAlarmInfo, dwBufLen, pUser);

            return true; //回调函数需要有返回，表示正常接收到数据
        }
        public void InitAlarm()
        {
            //设置透传报警信息类型
            CHCNetSDK_Win64.NET_DVR_LOCAL_GENERAL_CFG struLocalCfg = new CHCNetSDK_Win64.NET_DVR_LOCAL_GENERAL_CFG();
            struLocalCfg.byAlarmJsonPictureSeparate = 1;//控制JSON透传报警数据和图片是否分离，0-不分离(COMM_VCA_ALARM返回)，1-分离（分离后走COMM_ISAPI_ALARM回调返回）

            Int32 nSize = Marshal.SizeOf(struLocalCfg);
            IntPtr ptrLocalCfg = Marshal.AllocHGlobal(nSize);
            Marshal.StructureToPtr(struLocalCfg, ptrLocalCfg, false);

            if (!CHCNetSDK_Win64.NET_DVR_SetSDKLocalCfg(17, ptrLocalCfg))  //NET_DVR_LOCAL_CFG_TYPE_GENERAL
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                var strErr = "NET_DVR_SetSDKLocalCfg failed, error code= " + iLastErr;
                Console.WriteLine(strErr);
            }
            Marshal.FreeHGlobal(ptrLocalCfg);
            m_lAlarmHandle = -1;


            //设置异常消息回调函数
            if (m_fExceptionCB == null)
            {
                m_fExceptionCB = new CHCNetSDK_Win64.EXCEPYIONCALLBACK(cbExceptionCB);
            }
            CHCNetSDK_Win64.NET_DVR_SetExceptionCallBack_V30(0, IntPtr.Zero, m_fExceptionCB, IntPtr.Zero);


            //设置报警回调函数
            if (m_falarmData_V31 == null)
            {
                m_falarmData_V31 = new CHCNetSDK_Win64.MSGCallBack_V31(MsgCallback_V31);
            }
            CHCNetSDK_Win64.NET_DVR_SetDVRMessageCallBack_V31(m_falarmData_V31, IntPtr.Zero);
        }
        /// <summary>
        /// 设置布放
        /// </summary>
        /// <returns></returns>
        public override ReturnVideo SetAlarm()
        {
            InitAlarm();
            CHCNetSDK_Win64.NET_DVR_SETUPALARM_PARAM struAlarmParam = new CHCNetSDK_Win64.NET_DVR_SETUPALARM_PARAM();
            struAlarmParam.dwSize = (uint)Marshal.SizeOf(struAlarmParam);
            struAlarmParam.byLevel = 1; //0- 一级布防,1- 二级布防
            struAlarmParam.byAlarmInfoType = 1;//智能交通设备有效，新报警信息类型
            struAlarmParam.byFaceAlarmDetection = 1;//1-人脸侦测

            m_lAlarmHandle = CHCNetSDK_Win64.NET_DVR_SetupAlarmChan_V41(m_lUserID, ref struAlarmParam);
            if (m_lAlarmHandle < 0)
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                var strErr = "布防失败，错误号：" + iLastErr; //布防失败，输出错误号
                return new ReturnVideo { Success = false, Msg = strErr };
            }
            else
            {
                return new ReturnVideo { Success = true, Msg = "布防成功" };
            }
        }
        /// <summary>
        /// 撤销布放
        /// </summary>
        /// <returns></returns>
        public override ReturnVideo CloseAlarm()
        {
            if (m_lAlarmHandle > 0)
            {
                if (!CHCNetSDK_Win64.NET_DVR_CloseAlarmChan_V30(m_lUserID))
                {
                    iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                    var strErr = "撤防失败，错误号：" + iLastErr; //撤防失败，输出错误号
                    return new ReturnVideo { Success = false, Msg = strErr };
                }
                else
                {
                    m_lAlarmHandle = -1;
                    return new ReturnVideo { Success = true, Msg = "撤防成功" };
                }
            }
            else
            {
                return new ReturnVideo { Success = true, Msg = "未布防" };
            }
        }


        public override ReturnVideo AlarmStartListen()
        {
            string sLocalIP = this.localIP;
            ushort wLocalPort = this.localPort;

            if (m_falarmData == null)
            {
                m_falarmData = new CHCNetSDK_Win64.MSGCallBack(MsgCallback);
            }

            iListenHandle = CHCNetSDK_Win64.NET_DVR_StartListen_V30(sLocalIP, wLocalPort, m_falarmData, IntPtr.Zero);
            if (iListenHandle < 0)
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                var strErr = "启动监听失败，错误号：" + iLastErr; //撤防失败，输出错误号
                return new ReturnVideo { Success = false, Msg = strErr };
            }
            else
            {
                return new ReturnVideo { Success = true, Msg = "成功启动监听！" };
            }
        }


        public override ReturnVideo AlarmStopListen()
        {
            if (!CHCNetSDK_Win64.NET_DVR_StopListen_V30(iListenHandle))
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                var strErr = "停止监听失败，错误号：" + iLastErr; //撤防失败，输出错误号
                return new ReturnVideo { Success = false, Msg = strErr };
            }
            else
            {
                return new ReturnVideo { Success = true, Msg = "停止监听！" };
            }
        }


        public override void Zoom(int isStart, int isIn)
        {
            if (!CHCNetSDK_Win64.NET_DVR_PTZControl(m_lRealHandle, isIn == 1 ? (uint)CHCNetSDK_Win64.ZOOM_IN : (uint)CHCNetSDK_Win64.ZOOM_OUT, isStart == 1 ? (uint)0 : (uint)1))
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                var strErr = "调焦失败，错误号：" + iLastErr; //撤防失败，输出错误号
                Console.WriteLine(strErr);
            }
        }
        public override ReturnVideo Cruise(int isStart,int byCruiseRoute)
        {
            if (!CHCNetSDK_Win64.NET_DVR_PTZCruise(m_lRealHandle, isStart == 1 ? (uint)CHCNetSDK_Win64.RUN_SEQ : (uint)CHCNetSDK_Win64.STOP_SEQ, (byte)byCruiseRoute, 0, 0))
            {
                iLastErr = CHCNetSDK_Win64.NET_DVR_GetLastError();
                var strErr = "巡航失败，错误号：" + iLastErr; //撤防失败，输出错误号
                Console.WriteLine(strErr);
                return new ReturnVideo { Success = true, Msg= strErr };
            }
            return new ReturnVideo { Success = true };
        }




        public CHCNetSDK_Win64.MSGCallBack m_falarmData = null;
        /*********************************************************
        Function:	MSGCallBack
        Desc:		(回调函数)
        Input:	
        Output:	
        Return:	
        **********************************************************/
        public void MsgCallback(int lCommand, ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            try
            {
                //通过lCommand来判断接收到的报警信息类型，不同的lCommand对应不同的pAlarmInfo内容
                AlarmMessageHandle(lCommand, ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("MsgCallback:" + ex.Message);
            }
        }

        public void AlarmMessageHandle(int lCommand, ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            //通过lCommand来判断接收到的报警信息类型，不同的lCommand对应不同的pAlarmInfo内容
            switch (lCommand)
            {
                case CHCNetSDK_Win64.COMM_ALARM: //(DS-8000老设备)移动侦测、视频丢失、遮挡、IO信号量等报警信息
                    ProcessCommAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARM_V30://移动侦测、视频丢失、遮挡、IO信号量等报警信息
                    ProcessCommAlarm_V30(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARM_RULE://进出区域、入侵、徘徊、人员聚集等行为分析报警信息
                    ProcessCommAlarm_RULE(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_UPLOAD_PLATE_RESULT://交通抓拍结果上传(老报警信息类型)
                    ProcessCommAlarm_Plate(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ITS_PLATE_RESULT://交通抓拍结果上传(新报警信息类型)
                    ProcessCommAlarm_ITSPlate(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARM_TPS_REAL_TIME://交通抓拍结果上传(新报警信息类型)
                    ProcessCommAlarm_TPSRealInfo(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARM_TPS_STATISTICS://交通抓拍结果上传(新报警信息类型)
                    ProcessCommAlarm_TPSStatInfo(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARM_PDC://客流量统计报警信息
                    ProcessCommAlarm_PDC(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ITS_PARK_VEHICLE://客流量统计报警信息
                    ProcessCommAlarm_PARK(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_DIAGNOSIS_UPLOAD://VQD报警信息
                    ProcessCommAlarm_VQD(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_UPLOAD_FACESNAP_RESULT://人脸抓拍结果信息
                    ProcessCommAlarm_FaceSnap(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_SNAP_MATCH_ALARM://人脸比对结果信息
                    ProcessCommAlarm_FaceMatch(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARM_FACE_DETECTION://人脸侦测报警信息
                    ProcessCommAlarm_FaceDetect(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARMHOST_CID_ALARM://报警主机CID报警上传
                    ProcessCommAlarm_CIDAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_UPLOAD_VIDEO_INTERCOM_EVENT://可视对讲事件记录信息
                    ProcessCommAlarm_InterComEvent(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ALARM_ACS://门禁主机报警上传
                    ProcessCommAlarm_AcsAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ID_INFO_ALARM://身份证刷卡信息上传
                    ProcessCommAlarm_IDInfoAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_UPLOAD_AIOP_VIDEO://设备支持AI开放平台接入，上传视频检测数据
                    ProcessCommAlarm_AIOPVideo(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_UPLOAD_AIOP_PICTURE://设备支持AI开放平台接入，上传图片检测数据
                    ProcessCommAlarm_AIOPPicture(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK_Win64.COMM_ISAPI_ALARM://ISAPI报警信息上传
                    ProcessCommAlarm_ISAPIAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                default:
                    {
                        //报警设备IP地址
                        string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

                        //报警信息类型
                        string stringAlarm = "报警上传，信息类型：0x" + Convert.ToString(lCommand, 16);

                        //if (InvokeRequired)
                        //{
                        //    object[] paras = new object[3];
                        //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
                        //    paras[1] = strIP;
                        //    paras[2] = stringAlarm;
                        //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
                        //}
                        //else
                        //{
                        //    //创建该控件的主线程直接更新信息列表 
                        //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
                        //}
                        if (ReturnAlarm != null)
                            ReturnAlarm(strIP, stringAlarm, "DEFAULT", 0);
                    }
                    break;
            }
        }

        public void ProcessCommAlarm(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_ALARMINFO struAlarmInfo = new CHCNetSDK_Win64.NET_DVR_ALARMINFO();

            struAlarmInfo = (CHCNetSDK_Win64.NET_DVR_ALARMINFO)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_ALARMINFO));

            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');
            string stringAlarm = "";
            int i = 0;

            switch (struAlarmInfo.dwAlarmType)
            {
                case 0:
                    stringAlarm = "信号量报警，报警报警输入口：" + struAlarmInfo.dwAlarmInputNumber + "，触发录像通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwAlarmRelateChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 1:
                    stringAlarm = "硬盘满，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_DISKNUM; i++)
                    {
                        if (struAlarmInfo.dwDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 2:
                    stringAlarm = "信号丢失，报警通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 3:
                    stringAlarm = "移动侦测，报警通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 4:
                    stringAlarm = "硬盘未格式化，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_DISKNUM; i++)
                    {
                        if (struAlarmInfo.dwDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 5:
                    stringAlarm = "读写硬盘出错，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_DISKNUM; i++)
                    {
                        if (struAlarmInfo.dwDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 6:
                    stringAlarm = "遮挡报警，报警通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 7:
                    stringAlarm = "制式不匹配，报警通道";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 8:
                    stringAlarm = "非法访问";
                    break;
                default:
                    stringAlarm = "其他未知报警信息";
                    break;
            }

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString();
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM", struAlarmInfo.dwAlarmType);
        }

        private void ProcessCommAlarm_V30(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_ALARMINFO_V30 struAlarmInfoV30 = new CHCNetSDK_Win64.NET_DVR_ALARMINFO_V30();

            struAlarmInfoV30 = (CHCNetSDK_Win64.NET_DVR_ALARMINFO_V30)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_ALARMINFO_V30));

            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');
            string stringAlarm = "";
            int i;

            switch (struAlarmInfoV30.dwAlarmType)
            {
                case 0:
                    stringAlarm = "信号量报警，报警报警输入口：" + struAlarmInfoV30.dwAlarmInputNumber + "，触发录像通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byAlarmRelateChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + "\\";
                        }
                    }
                    break;
                case 1:
                    stringAlarm = "硬盘满，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_DISKNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " ";
                        }
                    }
                    break;
                case 2:
                    stringAlarm = "信号丢失，报警通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 3:
                    stringAlarm = "移动侦测，报警通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 4:
                    stringAlarm = "硬盘未格式化，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_DISKNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 5:
                    stringAlarm = "读写硬盘出错，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_DISKNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 6:
                    stringAlarm = "遮挡报警，报警通道：";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 7:
                    stringAlarm = "制式不匹配，报警通道";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 8:
                    stringAlarm = "非法访问";
                    break;
                case 9:
                    stringAlarm = "视频信号异常，报警通道";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 10:
                    stringAlarm = "录像/抓图异常，报警通道";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 11:
                    stringAlarm = "智能场景变化，报警通道";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 12:
                    stringAlarm = "阵列异常";
                    break;
                case 13:
                    stringAlarm = "前端/录像分辨率不匹配，报警通道";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 15:
                    stringAlarm = "智能侦测，报警通道";
                    for (i = 0; i < CHCNetSDK_Win64.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                default:
                    stringAlarm = "其他未知报警信息";
                    break;
            }

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString();
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM_V30", (int)struAlarmInfoV30.dwAlarmType);

        }

        CHCNetSDK_Win64.NET_VCA_TRAVERSE_PLANE m_struTraversePlane = new CHCNetSDK_Win64.NET_VCA_TRAVERSE_PLANE();
        CHCNetSDK_Win64.NET_VCA_AREA m_struVcaArea = new CHCNetSDK_Win64.NET_VCA_AREA();
        CHCNetSDK_Win64.NET_VCA_INTRUSION m_struIntrusion = new CHCNetSDK_Win64.NET_VCA_INTRUSION();

        private int iFileNumber = 0; //保存的文件个数

        
        private void ProcessCommAlarm_RULE(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_VCA_RULE_ALARM struRuleAlarmInfo = new CHCNetSDK_Win64.NET_VCA_RULE_ALARM();
            struRuleAlarmInfo = (CHCNetSDK_Win64.NET_VCA_RULE_ALARM)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_VCA_RULE_ALARM));

            //报警信息
            string stringAlarm = "";
            uint dwSize = (uint)Marshal.SizeOf(struRuleAlarmInfo.struRuleInfo.uEventParam);

            switch (struRuleAlarmInfo.struRuleInfo.wEventTypeEx)
            {
                case (ushort)CHCNetSDK_Win64.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_TRAVERSE_PLANE:
                    IntPtr ptrTraverseInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrTraverseInfo, false);
                    m_struTraversePlane = (CHCNetSDK_Win64.NET_VCA_TRAVERSE_PLANE)Marshal.PtrToStructure(ptrTraverseInfo, typeof(CHCNetSDK_Win64.NET_VCA_TRAVERSE_PLANE));
                    stringAlarm = "穿越警戒面，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    //警戒面边线起点坐标: (m_struTraversePlane.struPlaneBottom.struStart.fX, m_struTraversePlane.struPlaneBottom.struStart.fY)
                    //警戒面边线终点坐标: (m_struTraversePlane.struPlaneBottom.struEnd.fX, m_struTraversePlane.struPlaneBottom.struEnd.fY)
                    break;
                case (ushort)CHCNetSDK_Win64.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_ENTER_AREA:
                    IntPtr ptrEnterInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrEnterInfo, false);
                    m_struVcaArea = (CHCNetSDK_Win64.NET_VCA_AREA)Marshal.PtrToStructure(ptrEnterInfo, typeof(CHCNetSDK_Win64.NET_VCA_AREA));
                    stringAlarm = "目标进入区域，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    //m_struVcaArea.struRegion 多边形区域坐标
                    break;
                case (ushort)CHCNetSDK_Win64.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_EXIT_AREA:
                    IntPtr ptrExitInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrExitInfo, false);
                    m_struVcaArea = (CHCNetSDK_Win64.NET_VCA_AREA)Marshal.PtrToStructure(ptrExitInfo, typeof(CHCNetSDK_Win64.NET_VCA_AREA));
                    stringAlarm = "目标离开区域，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    //m_struVcaArea.struRegion 多边形区域坐标
                    break;
                case (ushort)CHCNetSDK_Win64.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_INTRUSION:
                    IntPtr ptrIntrusionInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrIntrusionInfo, false);
                    m_struIntrusion = (CHCNetSDK_Win64.NET_VCA_INTRUSION)Marshal.PtrToStructure(ptrIntrusionInfo, typeof(CHCNetSDK_Win64.NET_VCA_INTRUSION));

                    int i = 0;
                    string strRegion = "";
                    for (i = 0; i < m_struIntrusion.struRegion.dwPointNum; i++)
                    {
                        strRegion = strRegion + "(" + m_struIntrusion.struRegion.struPos[i].fX + "," + m_struIntrusion.struRegion.struPos[i].fY + ")";
                    }
                    stringAlarm = "周界入侵，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID + "，区域范围：" + strRegion;
                    //m_struIntrusion.struRegion 多边形区域坐标
                    break;
                default:
                    stringAlarm = "其他行为分析报警，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    break;
            }


            //报警图片保存
            if (struRuleAlarmInfo.dwPicDataLen > 0)
            {
                // ".\\picture\\UserID_" + pAlarmer.lUserID + "_行为分析_" + iFileNumber + ".jpg";
                SaveAlarmPic("UserID_" + pAlarmer.lUserID + "_行为分析_" + iFileNumber + ".jpg", (int)struRuleAlarmInfo.dwPicDataLen, struRuleAlarmInfo.pImage);
                iFileNumber++;
            }

            //报警时间：年月日时分秒
            string strTimeYear = ((struRuleAlarmInfo.dwAbsTime >> 26) + 2000).ToString();
            string strTimeMonth = ((struRuleAlarmInfo.dwAbsTime >> 22) & 15).ToString("d2");
            string strTimeDay = ((struRuleAlarmInfo.dwAbsTime >> 17) & 31).ToString("d2");
            string strTimeHour = ((struRuleAlarmInfo.dwAbsTime >> 12) & 31).ToString("d2");
            string strTimeMinute = ((struRuleAlarmInfo.dwAbsTime >> 6) & 63).ToString("d2");
            string strTimeSecond = ((struRuleAlarmInfo.dwAbsTime >> 0) & 63).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(struRuleAlarmInfo.struDevInfo.struDevIP.sIpV4).TrimEnd('\0');

            //将报警信息添加进列表
            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = strTime;
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(strTime, strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM_RULE", (int)struRuleAlarmInfo.struRuleInfo.wEventTypeEx);
        }

        private void ProcessCommAlarm_Plate(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_PLATE_RESULT struPlateResultInfo = new CHCNetSDK_Win64.NET_DVR_PLATE_RESULT();
            uint dwSize = (uint)Marshal.SizeOf(struPlateResultInfo);

            struPlateResultInfo = (CHCNetSDK_Win64.NET_DVR_PLATE_RESULT)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_PLATE_RESULT));

            //保存抓拍图片
            string str = "";
            if (struPlateResultInfo.byResultType == 1 && struPlateResultInfo.dwPicLen != 0)
            {
                //str = ".\\picture\\Plate_UserID_" + pAlarmer.lUserID + "_近景图_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struPlateResultInfo.dwPicLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struPlateResultInfo.pBuffer1, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("Plate_UserID_" + pAlarmer.lUserID + "_近景图_" + iFileNumber + ".jpg", (int)struPlateResultInfo.dwPicLen, struPlateResultInfo.pBuffer1);
                iFileNumber++;
            }
            if (struPlateResultInfo.dwPicPlateLen != 0)
            {
                //str = ".\\picture\\Plate_UserID_" + pAlarmer.lUserID + "_车牌图_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struPlateResultInfo.dwPicPlateLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struPlateResultInfo.pBuffer2, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("Plate_UserID_" + pAlarmer.lUserID + "_车牌图_" + iFileNumber + ".jpg", (int)struPlateResultInfo.dwPicPlateLen, struPlateResultInfo.pBuffer2);
                iFileNumber++;
            }
            if (struPlateResultInfo.dwFarCarPicLen != 0)
            {
                //str = ".\\picture\\Plate_UserID_" + pAlarmer.lUserID + "_远景图_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struPlateResultInfo.dwFarCarPicLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struPlateResultInfo.pBuffer5, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("Plate_UserID_" + pAlarmer.lUserID + "_远景图_" + iFileNumber + ".jpg", (int)struPlateResultInfo.dwFarCarPicLen, struPlateResultInfo.pBuffer5);
                iFileNumber++;
            }

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //抓拍时间：年月日时分秒
            string strTimeYear = System.Text.Encoding.UTF8.GetString(struPlateResultInfo.byAbsTime).TrimEnd('\0');

            //上传结果
            string stringPlateLicense = System.Text.Encoding.GetEncoding("GBK").GetString(struPlateResultInfo.struPlateInfo.sLicense).TrimEnd('\0');
            string stringAlarm = "抓拍上传，" + "车牌：" + stringPlateLicense + "，车辆序号：" + struPlateResultInfo.struVehicleInfo.dwIndex;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = strTimeYear; //当前PC系统时间为DateTime.Now.ToString();
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_UPLOAD_PLATE_RESULT", 0);
        }


        private void ProcessCommAlarm_ITSPlate(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_ITS_PLATE_RESULT struITSPlateResult = new CHCNetSDK_Win64.NET_ITS_PLATE_RESULT();
            uint dwSize = (uint)Marshal.SizeOf(struITSPlateResult);

            struITSPlateResult = (CHCNetSDK_Win64.NET_ITS_PLATE_RESULT)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_ITS_PLATE_RESULT));

            //保存抓拍图片
            for (int i = 0; i < struITSPlateResult.dwPicNum; i++)
            {
                if (struITSPlateResult.struPicInfo[i].dwDataLen != 0)
                {
                    //string str = ".\\picture\\ITS_UserID_[" + pAlarmer.lUserID + "]_Pictype_" + struITSPlateResult.struPicInfo[i].byType
                    //    + "_PicNum[" + (i + 1) + "]_" + iFileNumber + ".jpg";
                    //FileStream fs = new FileStream(str, FileMode.Create);
                    //int iLen = (int)struITSPlateResult.struPicInfo[i].dwDataLen;
                    //byte[] by = new byte[iLen];
                    //Marshal.Copy(struITSPlateResult.struPicInfo[i].pBuffer, by, 0, iLen);
                    //fs.Write(by, 0, iLen);
                    //fs.Close();
                    SaveAlarmPic("ITS_UserID_[" + pAlarmer.lUserID + "]_Pictype_" + struITSPlateResult.struPicInfo[i].byType
                        + "_PicNum[" + (i + 1) + "]_" + iFileNumber + ".jpg", (int)struITSPlateResult.struPicInfo[i].dwDataLen, struITSPlateResult.struPicInfo[i].pBuffer);
                    iFileNumber++;
                }
            }
            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //抓拍时间：年月日时分秒
            string strTimeYear = string.Format("{0:D4}", struITSPlateResult.struSnapFirstPicTime.wYear) +
                string.Format("{0:D2}", struITSPlateResult.struSnapFirstPicTime.byMonth) +
                string.Format("{0:D2}", struITSPlateResult.struSnapFirstPicTime.byDay) + " "
                + string.Format("{0:D2}", struITSPlateResult.struSnapFirstPicTime.byHour) + ":"
                + string.Format("{0:D2}", struITSPlateResult.struSnapFirstPicTime.byMinute) + ":"
                + string.Format("{0:D2}", struITSPlateResult.struSnapFirstPicTime.bySecond) + ":"
                + string.Format("{0:D3}", struITSPlateResult.struSnapFirstPicTime.wMilliSec);

            //上传结果
            string stringPlateLicense = System.Text.Encoding.GetEncoding("GBK").GetString(struITSPlateResult.struPlateInfo.sLicense).TrimEnd('\0');
            string stringAlarm = "抓拍上传，" + "车牌：" + stringPlateLicense + "，车辆序号：" + struITSPlateResult.struVehicleInfo.dwIndex;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = strTimeYear;//当前系统时间为：DateTime.Now.ToString();
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ITS_PLATE_RESULT", 0);
        }

        private void ProcessCommAlarm_TPSStatInfo(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_TPS_STATISTICS_INFO struTPSStatInfo = new CHCNetSDK_Win64.NET_DVR_TPS_STATISTICS_INFO();
            uint dwSize = (uint)Marshal.SizeOf(struTPSStatInfo);

            struTPSStatInfo = (CHCNetSDK_Win64.NET_DVR_TPS_STATISTICS_INFO)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_TPS_STATISTICS_INFO));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //抓拍时间：年月日时分秒
            string strTimeYear = string.Format("{0:D4}", struTPSStatInfo.struTPSStatisticsInfo.struStartTime.wYear) +
                string.Format("{0:D2}", struTPSStatInfo.struTPSStatisticsInfo.struStartTime.byMonth) +
                string.Format("{0:D2}", struTPSStatInfo.struTPSStatisticsInfo.struStartTime.byDay) + " "
                + string.Format("{0:D2}", struTPSStatInfo.struTPSStatisticsInfo.struStartTime.byHour) + ":"
                + string.Format("{0:D2}", struTPSStatInfo.struTPSStatisticsInfo.struStartTime.byMinute) + ":"
                + string.Format("{0:D2}", struTPSStatInfo.struTPSStatisticsInfo.struStartTime.bySecond) + ":"
                + string.Format("{0:D3}", struTPSStatInfo.struTPSStatisticsInfo.struStartTime.wMilliSec);

            //上传结果
            string stringAlarm = "TPS统计过车数据，" + "通道号：" + struTPSStatInfo.dwChan +
                "，开始码：" + struTPSStatInfo.struTPSStatisticsInfo.byStart +
                "，命令号：" + struTPSStatInfo.struTPSStatisticsInfo.byCMD +
                "，统计开始时间：" + strTimeYear +
                "，统计时间(秒)：" + struTPSStatInfo.struTPSStatisticsInfo.dwSamplePeriod;


            for (int i = 0; i < CHCNetSDK_Win64.MAX_TPS_RULE; i++)
            {
                stringAlarm = stringAlarm + "车道号: " + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].byLane +
                    "，车道过车平均速度:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].bySpeed +
                    "，小型车数量:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].dwLightVehicle +
                    "，中型车数量:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].dwMidVehicle +
                    "，重型车数量:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].dwHeavyVehicle +
                    "，车头时距:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].dwTimeHeadway +
                    "，车头间距:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].dwSpaceHeadway +
                    "，空间占有率:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].fSpaceOccupyRation +
                    "，时间占有率:" + struTPSStatInfo.struTPSStatisticsInfo.struLaneParam[i].fTimeOccupyRation;
            }

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString();//当前系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM_TPS_STATISTICS", 1);
        }
        CHCNetSDK_Win64.UNION_STATFRAME m_struStatFrame = new CHCNetSDK_Win64.UNION_STATFRAME();
        CHCNetSDK_Win64.UNION_STATTIME m_struStatTime = new CHCNetSDK_Win64.UNION_STATTIME();
        private void ProcessCommAlarm_PDC(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_PDC_ALRAM_INFO struPDCInfo = new CHCNetSDK_Win64.NET_DVR_PDC_ALRAM_INFO();
            uint dwSize = (uint)Marshal.SizeOf(struPDCInfo);
            struPDCInfo = (CHCNetSDK_Win64.NET_DVR_PDC_ALRAM_INFO)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_PDC_ALRAM_INFO));

            string stringAlarm = "客流量统计，进入人数：" + struPDCInfo.dwEnterNum + "，离开人数：" + struPDCInfo.dwLeaveNum;

            uint dwUnionSize = (uint)Marshal.SizeOf(struPDCInfo.uStatModeParam);
            IntPtr ptrPDCUnion = Marshal.AllocHGlobal((Int32)dwUnionSize);
            Marshal.StructureToPtr(struPDCInfo.uStatModeParam, ptrPDCUnion, false);

            if (struPDCInfo.byMode == 0) //单帧统计结果，此处为UTC时间
            {
                m_struStatFrame = (CHCNetSDK_Win64.UNION_STATFRAME)Marshal.PtrToStructure(ptrPDCUnion, typeof(CHCNetSDK_Win64.UNION_STATFRAME));
                stringAlarm = stringAlarm + "，单帧统计，相对时标：" + m_struStatFrame.dwRelativeTime + "，绝对时标：" + m_struStatFrame.dwAbsTime;
            }
            if (struPDCInfo.byMode == 1) //最小时间段统计结果
            {
                m_struStatTime = (CHCNetSDK_Win64.UNION_STATTIME)Marshal.PtrToStructure(ptrPDCUnion, typeof(CHCNetSDK_Win64.UNION_STATTIME));

                //开始时间
                string strStartTime = string.Format("{0:D4}", m_struStatTime.tmStart.dwYear) +
                string.Format("{0:D2}", m_struStatTime.tmStart.dwMonth) +
                string.Format("{0:D2}", m_struStatTime.tmStart.dwDay) + " "
                + string.Format("{0:D2}", m_struStatTime.tmStart.dwHour) + ":"
                + string.Format("{0:D2}", m_struStatTime.tmStart.dwMinute) + ":"
                + string.Format("{0:D2}", m_struStatTime.tmStart.dwSecond);

                //结束时间
                string strEndTime = string.Format("{0:D4}", m_struStatTime.tmEnd.dwYear) +
                string.Format("{0:D2}", m_struStatTime.tmEnd.dwMonth) +
                string.Format("{0:D2}", m_struStatTime.tmEnd.dwDay) + " "
                + string.Format("{0:D2}", m_struStatTime.tmEnd.dwHour) + ":"
                + string.Format("{0:D2}", m_struStatTime.tmEnd.dwMinute) + ":"
                + string.Format("{0:D2}", m_struStatTime.tmEnd.dwSecond);

                stringAlarm = stringAlarm + "，最小时间段统计，开始时间：" + strStartTime + "，结束时间：" + strEndTime;
            }
            Marshal.FreeHGlobal(ptrPDCUnion);

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');


            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM_PDC", 0);
        }

        private void ProcessCommAlarm_TPSRealInfo(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_TPS_REAL_TIME_INFO struTPSInfo = new CHCNetSDK_Win64.NET_DVR_TPS_REAL_TIME_INFO();
            uint dwSize = (uint)Marshal.SizeOf(struTPSInfo);

            struTPSInfo = (CHCNetSDK_Win64.NET_DVR_TPS_REAL_TIME_INFO)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_TPS_REAL_TIME_INFO));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //抓拍时间：年月日时分秒
            string strTimeYear = string.Format("{0:D4}", struTPSInfo.struTime.wYear) +
                string.Format("{0:D2}", struTPSInfo.struTime.byMonth) +
                string.Format("{0:D2}", struTPSInfo.struTime.byDay) + " "
                + string.Format("{0:D2}", struTPSInfo.struTime.byHour) + ":"
                + string.Format("{0:D2}", struTPSInfo.struTime.byMinute) + ":"
                + string.Format("{0:D2}", struTPSInfo.struTime.bySecond) + ":"
                + string.Format("{0:D3}", struTPSInfo.struTime.wMilliSec);

            //上传结果
            string stringAlarm = "TPS实时过车数据，" + "通道号：" + struTPSInfo.dwChan +
                "，设备ID：" + struTPSInfo.struTPSRealTimeInfo.wDeviceID +
                "，开始码：" + struTPSInfo.struTPSRealTimeInfo.byStart +
                "，命令号：" + struTPSInfo.struTPSRealTimeInfo.byCMD +
                "，对应车道：" + struTPSInfo.struTPSRealTimeInfo.byLane +
                "，对应车速：" + struTPSInfo.struTPSRealTimeInfo.bySpeed +
                "，byLaneState：" + struTPSInfo.struTPSRealTimeInfo.byLaneState +
                "，byQueueLen：" + struTPSInfo.struTPSRealTimeInfo.byQueueLen +
                "，wLoopState：" + struTPSInfo.struTPSRealTimeInfo.wLoopState +
                "，wStateMask：" + struTPSInfo.struTPSRealTimeInfo.wStateMask +
                "，dwDownwardFlow：" + struTPSInfo.struTPSRealTimeInfo.dwDownwardFlow +
                "，dwUpwardFlow：" + struTPSInfo.struTPSRealTimeInfo.dwUpwardFlow +
                "，byJamLevel：" + struTPSInfo.struTPSRealTimeInfo.byJamLevel;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = strTimeYear;//当前系统时间为：DateTime.Now.ToString();
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM_TPS_REAL_TIME", 0);
        }


        private void ProcessCommAlarm_PARK(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_ITS_PARK_VEHICLE struParkInfo = new CHCNetSDK_Win64.NET_ITS_PARK_VEHICLE();
            uint dwSize = (uint)Marshal.SizeOf(struParkInfo);
            struParkInfo = (CHCNetSDK_Win64.NET_ITS_PARK_VEHICLE)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_ITS_PARK_VEHICLE));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //保存抓拍图片
            for (int i = 0; i < struParkInfo.dwPicNum; i++)
            {
                if ((struParkInfo.struPicInfo[i].dwDataLen != 0) && (struParkInfo.struPicInfo[i].pBuffer != IntPtr.Zero))
                {
                    //string str = ".\\picture\\Device_Park_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_Pictype_" + struParkInfo.struPicInfo[i].byType
                    //    + "_PicNum[" + (i + 1) + "]_" + iFileNumber + ".jpg";
                    //FileStream fs = new FileStream(str, FileMode.Create);
                    //int iLen = (int)struParkInfo.struPicInfo[i].dwDataLen;
                    //byte[] by = new byte[iLen];
                    //Marshal.Copy(struParkInfo.struPicInfo[i].pBuffer, by, 0, iLen);
                    //fs.Write(by, 0, iLen);
                    //fs.Close();
                    SaveAlarmPic("Device_Park_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_Pictype_" + struParkInfo.struPicInfo[i].byType
                        + "_PicNum[" + (i + 1) + "]_" + iFileNumber + ".jpg", (int)struParkInfo.struPicInfo[i].dwDataLen, struParkInfo.struPicInfo[i].pBuffer);
                    iFileNumber++;
                }
            }

            string stringAlarm = "停车场数据上传，异常状态：" + struParkInfo.byParkError + "，车位编号：" + struParkInfo.byParkingNo +
                ", 车辆状态：" + struParkInfo.byLocationStatus + "，车牌号码：" +
                System.Text.Encoding.GetEncoding("GBK").GetString(struParkInfo.struPlateInfo.sLicense).TrimEnd('\0');

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ITS_PARK_VEHICLE", 0);
        }

        private void ProcessCommAlarm_VQD(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_DIAGNOSIS_UPLOAD struVQDInfo = new CHCNetSDK_Win64.NET_DVR_DIAGNOSIS_UPLOAD();
            uint dwSize = (uint)Marshal.SizeOf(struVQDInfo);
            struVQDInfo = (CHCNetSDK_Win64.NET_DVR_DIAGNOSIS_UPLOAD)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_DIAGNOSIS_UPLOAD));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //开始时间
            string strCheckTime = string.Format("{0:D4}", struVQDInfo.struCheckTime.dwYear) +
            string.Format("{0:D2}", struVQDInfo.struCheckTime.dwMonth) +
            string.Format("{0:D2}", struVQDInfo.struCheckTime.dwDay) + " "
            + string.Format("{0:D2}", struVQDInfo.struCheckTime.dwHour) + ":"
            + string.Format("{0:D2}", struVQDInfo.struCheckTime.dwMinute) + ":"
            + string.Format("{0:D2}", struVQDInfo.struCheckTime.dwSecond);

            string stringAlarm = "视频质量诊断结果，流ID：" + struVQDInfo.sStreamID + "，监测点IP：" + struVQDInfo.sMonitorIP + "，监控点通道号：" + struVQDInfo.dwChanIndex +
                "，检测时间：" + strCheckTime + "，byResult：" + struVQDInfo.byResult + "，bySignalResult：" + struVQDInfo.bySignalResult + "，byBlurResult：" + struVQDInfo.byBlurResult;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_DIAGNOSIS_UPLOAD", 0);
        }

        private void ProcessCommAlarm_FaceSnap(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_VCA_FACESNAP_RESULT struFaceSnapInfo = new CHCNetSDK_Win64.NET_VCA_FACESNAP_RESULT();
            uint dwSize = (uint)Marshal.SizeOf(struFaceSnapInfo);
            struFaceSnapInfo = (CHCNetSDK_Win64.NET_VCA_FACESNAP_RESULT)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_VCA_FACESNAP_RESULT));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //保存抓拍图片数据
            if ((struFaceSnapInfo.dwBackgroundPicLen != 0) && (struFaceSnapInfo.pBuffer2 != IntPtr.Zero))
            {
                //string str = ".\\picture\\FaceSnap_CapPic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struFaceSnapInfo.dwBackgroundPicLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struFaceSnapInfo.pBuffer2, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("FaceSnap_CapPic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg", (int)struFaceSnapInfo.dwBackgroundPicLen, struFaceSnapInfo.pBuffer2);
                iFileNumber++;
            }

            //报警时间：年月日时分秒
            string strTimeYear = ((struFaceSnapInfo.dwAbsTime >> 26) + 2000).ToString();
            string strTimeMonth = ((struFaceSnapInfo.dwAbsTime >> 22) & 15).ToString("d2");
            string strTimeDay = ((struFaceSnapInfo.dwAbsTime >> 17) & 31).ToString("d2");
            string strTimeHour = ((struFaceSnapInfo.dwAbsTime >> 12) & 31).ToString("d2");
            string strTimeMinute = ((struFaceSnapInfo.dwAbsTime >> 6) & 63).ToString("d2");
            string strTimeSecond = ((struFaceSnapInfo.dwAbsTime >> 0) & 63).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "人脸抓拍结果，前端设备：" + System.Text.Encoding.UTF8.GetString(struFaceSnapInfo.struDevInfo.struDevIP.sIpV4).TrimEnd('\0') +
                "，通道号：" + struFaceSnapInfo.struDevInfo.byIvmsChannel + "，报警时间：" + strTime;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_UPLOAD_FACESNAP_RESULT", 0);
        }

        private void ProcessCommAlarm_FaceMatch(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_VCA_FACESNAP_MATCH_ALARM struFaceMatchAlarm = new CHCNetSDK_Win64.NET_VCA_FACESNAP_MATCH_ALARM();
            uint dwSize = (uint)Marshal.SizeOf(struFaceMatchAlarm);
            struFaceMatchAlarm = (CHCNetSDK_Win64.NET_VCA_FACESNAP_MATCH_ALARM)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_VCA_FACESNAP_MATCH_ALARM));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //保存抓拍人脸子图图片数据
            if ((struFaceMatchAlarm.struSnapInfo.dwSnapFacePicLen != 0) && (struFaceMatchAlarm.struSnapInfo.pBuffer1 != IntPtr.Zero))
            {
                //string str = ".\\picture\\FaceMatch_FacePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struFaceMatchAlarm.struSnapInfo.dwSnapFacePicLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struFaceMatchAlarm.struSnapInfo.pBuffer1, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("FaceMatch_FacePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg", (int)struFaceMatchAlarm.struSnapInfo.dwSnapFacePicLen, struFaceMatchAlarm.struSnapInfo.pBuffer1);
                iFileNumber++;
            }

            //保存比对结果人脸库人脸图片数据
            if ((struFaceMatchAlarm.struBlackListInfo.dwBlackListPicLen != 0) && (struFaceMatchAlarm.struBlackListInfo.pBuffer1 != IntPtr.Zero))
            {
                //string str = ".\\picture\\FaceMatch_BlackListPic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                //    "_fSimilarity[" + struFaceMatchAlarm.fSimilarity + "]_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struFaceMatchAlarm.struBlackListInfo.dwBlackListPicLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struFaceMatchAlarm.struBlackListInfo.pBuffer1, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("FaceMatch_BlackListPic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                    "_fSimilarity[" + struFaceMatchAlarm.fSimilarity + "]_" + iFileNumber + ".jpg", (int)struFaceMatchAlarm.struBlackListInfo.dwBlackListPicLen, struFaceMatchAlarm.struBlackListInfo.pBuffer1);
                iFileNumber++;
            }

            //抓拍时间：年月日时分秒
            string strTimeYear = ((struFaceMatchAlarm.struSnapInfo.dwAbsTime >> 26) + 2000).ToString();
            string strTimeMonth = ((struFaceMatchAlarm.struSnapInfo.dwAbsTime >> 22) & 15).ToString("d2");
            string strTimeDay = ((struFaceMatchAlarm.struSnapInfo.dwAbsTime >> 17) & 31).ToString("d2");
            string strTimeHour = ((struFaceMatchAlarm.struSnapInfo.dwAbsTime >> 12) & 31).ToString("d2");
            string strTimeMinute = ((struFaceMatchAlarm.struSnapInfo.dwAbsTime >> 6) & 63).ToString("d2");
            string strTimeSecond = ((struFaceMatchAlarm.struSnapInfo.dwAbsTime >> 0) & 63).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "人脸比对报警，抓拍设备：" + System.Text.Encoding.UTF8.GetString(struFaceMatchAlarm.struSnapInfo.struDevInfo.struDevIP.sIpV4).TrimEnd('\0') + "，抓拍时间："
                + strTime + "，相似度：" + struFaceMatchAlarm.fSimilarity;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_SNAP_MATCH_ALARM", 1);
        }

        private void ProcessCommAlarm_FaceDetect(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_FACE_DETECTION struFaceDetectInfo = new CHCNetSDK_Win64.NET_DVR_FACE_DETECTION();
            uint dwSize = (uint)Marshal.SizeOf(struFaceDetectInfo);
            struFaceDetectInfo = (CHCNetSDK_Win64.NET_DVR_FACE_DETECTION)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_FACE_DETECTION));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0').TrimEnd('\0');

            //报警时间：年月日时分秒
            string strTimeYear = ((struFaceDetectInfo.dwAbsTime >> 26) + 2000).ToString();
            string strTimeMonth = ((struFaceDetectInfo.dwAbsTime >> 22) & 15).ToString("d2");
            string strTimeDay = ((struFaceDetectInfo.dwAbsTime >> 17) & 31).ToString("d2");
            string strTimeHour = ((struFaceDetectInfo.dwAbsTime >> 12) & 31).ToString("d2");
            string strTimeMinute = ((struFaceDetectInfo.dwAbsTime >> 6) & 63).ToString("d2");
            string strTimeSecond = ((struFaceDetectInfo.dwAbsTime >> 0) & 63).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "人脸抓拍结果结果，前端设备：" + System.Text.Encoding.UTF8.GetString(struFaceDetectInfo.struDevInfo.struDevIP.sIpV4) + "，报警时间：" + strTime;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM_FACE_DETECTION", 2);
        }

        private void ProcessCommAlarm_CIDAlarm(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_CID_ALARM struCIDAlarm = new CHCNetSDK_Win64.NET_DVR_CID_ALARM();
            uint dwSize = (uint)Marshal.SizeOf(struCIDAlarm);
            struCIDAlarm = (CHCNetSDK_Win64.NET_DVR_CID_ALARM)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_CID_ALARM));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //报警时间：年月日时分秒
            string strTimeYear = (struCIDAlarm.struTriggerTime.wYear).ToString();
            string strTimeMonth = (struCIDAlarm.struTriggerTime.byMonth).ToString("d2");
            string strTimeDay = (struCIDAlarm.struTriggerTime.byDay).ToString("d2");
            string strTimeHour = (struCIDAlarm.struTriggerTime.byHour).ToString("d2");
            string strTimeMinute = (struCIDAlarm.struTriggerTime.byMinute).ToString("d2");
            string strTimeSecond = (struCIDAlarm.struTriggerTime.bySecond).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "报警主机CID报告，sCIDCode：" + System.Text.Encoding.UTF8.GetString(struCIDAlarm.sCIDCode).TrimEnd('\0')
                + "，sCIDDescribe：" + System.Text.Encoding.UTF8.GetString(struCIDAlarm.sCIDDescribe).TrimEnd('\0')
                + "，报告类型：" + struCIDAlarm.byReportType + "，防区号：" + struCIDAlarm.wDefenceNo + "，报警触发时间：" + strTime;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARMHOST_CID_ALARM", 0);
        }

        private void ProcessCommAlarm_InterComEvent(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_VIDEO_INTERCOM_EVENT struInterComEvent = new CHCNetSDK_Win64.NET_DVR_VIDEO_INTERCOM_EVENT();
            uint dwSize = (uint)Marshal.SizeOf(struInterComEvent);
            struInterComEvent = (CHCNetSDK_Win64.NET_DVR_VIDEO_INTERCOM_EVENT)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_VIDEO_INTERCOM_EVENT));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            if (struInterComEvent.byEventType == 3)
            {
                CHCNetSDK_Win64.NET_DVR_AUTH_INFO struAuthInfo = new CHCNetSDK_Win64.NET_DVR_AUTH_INFO();
                int dwUnionSize = Marshal.SizeOf(struInterComEvent.uEventInfo);
                IntPtr ptrAuthInfo = Marshal.AllocHGlobal(dwUnionSize);
                Marshal.StructureToPtr(struInterComEvent.uEventInfo, ptrAuthInfo, false);
                struAuthInfo = (CHCNetSDK_Win64.NET_DVR_AUTH_INFO)Marshal.PtrToStructure(ptrAuthInfo, typeof(CHCNetSDK_Win64.NET_DVR_AUTH_INFO));
                Marshal.FreeHGlobal(ptrAuthInfo);

                //保存抓拍图片
                if ((struAuthInfo.dwPicDataLen != 0) && (struAuthInfo.pImage != IntPtr.Zero))
                {
                    //string str = ".\\picture\\Device_InterCom_CapturePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg";
                    //FileStream fs = new FileStream(str, FileMode.Create);
                    //int iLen = (int)struAuthInfo.dwPicDataLen;
                    //byte[] by = new byte[iLen];
                    //Marshal.Copy(struAuthInfo.pImage, by, 0, iLen);
                    //fs.Write(by, 0, iLen);
                    //fs.Close();
                    SaveAlarmPic("Device_InterCom_CapturePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg", (int)struAuthInfo.dwPicDataLen, struAuthInfo.pImage);
                    iFileNumber++;
                }
            }

            //报警时间：年月日时分秒
            string strTimeYear = (struInterComEvent.struTime.wYear).ToString();
            string strTimeMonth = (struInterComEvent.struTime.byMonth).ToString("d2");
            string strTimeDay = (struInterComEvent.struTime.byDay).ToString("d2");
            string strTimeHour = (struInterComEvent.struTime.byHour).ToString("d2");
            string strTimeMinute = (struInterComEvent.struTime.byMinute).ToString("d2");
            string strTimeSecond = (struInterComEvent.struTime.bySecond).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "可视对讲事件，byEventType：" + struInterComEvent.byEventType + "，设备编号："
                + System.Text.Encoding.UTF8.GetString(struInterComEvent.byDevNumber).TrimEnd('\0') + "，报警触发时间：" + strTime;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_UPLOAD_VIDEO_INTERCOM_EVENT", 0);

        }

        private void ProcessCommAlarm_AcsAlarm(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_ACS_ALARM_INFO struAcsAlarm = new CHCNetSDK_Win64.NET_DVR_ACS_ALARM_INFO();
            uint dwSize = (uint)Marshal.SizeOf(struAcsAlarm);
            struAcsAlarm = (CHCNetSDK_Win64.NET_DVR_ACS_ALARM_INFO)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_ACS_ALARM_INFO));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //保存抓拍图片
            if ((struAcsAlarm.dwPicDataLen != 0) && (struAcsAlarm.pPicData != IntPtr.Zero))
            {
                //string str = ".\\picture\\Device_Acs_CapturePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struAcsAlarm.dwPicDataLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struAcsAlarm.pPicData, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("Device_Acs_CapturePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg", (int)struAcsAlarm.dwPicDataLen, struAcsAlarm.pPicData);
                iFileNumber++;
            }

            //报警时间：年月日时分秒
            string strTimeYear = (struAcsAlarm.struTime.dwYear).ToString();
            string strTimeMonth = (struAcsAlarm.struTime.dwMonth).ToString("d2");
            string strTimeDay = (struAcsAlarm.struTime.dwDay).ToString("d2");
            string strTimeHour = (struAcsAlarm.struTime.dwHour).ToString("d2");
            string strTimeMinute = (struAcsAlarm.struTime.dwMinute).ToString("d2");
            string strTimeSecond = (struAcsAlarm.struTime.dwSecond).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "门禁主机报警信息，dwMajor：" + struAcsAlarm.dwMajor + "，dwMinor：" + struAcsAlarm.dwMinor + "，卡号："
                + System.Text.Encoding.UTF8.GetString(struAcsAlarm.struAcsEventInfo.byCardNo).TrimEnd('\0') + "，读卡器编号：" +
                struAcsAlarm.struAcsEventInfo.dwCardReaderNo + "，报警触发时间：" + strTime;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ALARM_ACS", 0);

        }

        private void ProcessCommAlarm_IDInfoAlarm(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_ID_CARD_INFO_ALARM struIDInfoAlarm = new CHCNetSDK_Win64.NET_DVR_ID_CARD_INFO_ALARM();
            uint dwSize = (uint)Marshal.SizeOf(struIDInfoAlarm);
            struIDInfoAlarm = (CHCNetSDK_Win64.NET_DVR_ID_CARD_INFO_ALARM)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_ID_CARD_INFO_ALARM));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //保存抓拍图片
            if ((struIDInfoAlarm.dwCapturePicDataLen != 0) && (struIDInfoAlarm.pCapturePicData != IntPtr.Zero))
            {
                //string str = ".\\picture\\Device_ID_CapturePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struIDInfoAlarm.dwCapturePicDataLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struIDInfoAlarm.pCapturePicData, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("Device_ID_CapturePic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg", (int)struIDInfoAlarm.dwCapturePicDataLen, struIDInfoAlarm.pCapturePicData);
                iFileNumber++;
            }

            //保存身份证图片数据
            if ((struIDInfoAlarm.dwPicDataLen != 0) && (struIDInfoAlarm.pPicData != IntPtr.Zero))
            {
                //string str = ".\\picture\\Device_ID_IDPic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struIDInfoAlarm.dwPicDataLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struIDInfoAlarm.pPicData, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("Device_ID_IDPic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".jpg", (int)struIDInfoAlarm.dwPicDataLen, struIDInfoAlarm.pPicData);
                iFileNumber++;
            }

            //保存指纹数据
            if ((struIDInfoAlarm.dwFingerPrintDataLen != 0) && (struIDInfoAlarm.pFingerPrintData != IntPtr.Zero))
            {
                //string str = ".\\picture\\Device_ID_FingerPrint_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".data";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struIDInfoAlarm.dwFingerPrintDataLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struIDInfoAlarm.pFingerPrintData, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("Device_ID_FingerPrint_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".data", (int)struIDInfoAlarm.dwFingerPrintDataLen, struIDInfoAlarm.pFingerPrintData);
                iFileNumber++;
            }

            //报警时间：年月日时分秒
            string strTimeYear = (struIDInfoAlarm.struSwipeTime.wYear).ToString();
            string strTimeMonth = (struIDInfoAlarm.struSwipeTime.byMonth).ToString("d2");
            string strTimeDay = (struIDInfoAlarm.struSwipeTime.byDay).ToString("d2");
            string strTimeHour = (struIDInfoAlarm.struSwipeTime.byHour).ToString("d2");
            string strTimeMinute = (struIDInfoAlarm.struSwipeTime.byMinute).ToString("d2");
            string strTimeSecond = (struIDInfoAlarm.struSwipeTime.bySecond).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "身份证刷卡信息，dwMajor：" + struIDInfoAlarm.dwMajor + "，dwMinor：" + struIDInfoAlarm.dwMinor
                + "，身份证号：" + System.Text.Encoding.UTF8.GetString(struIDInfoAlarm.struIDCardCfg.byIDNum).TrimEnd('\0') +
                "，姓名：" + System.Text.Encoding.UTF8.GetString(struIDInfoAlarm.struIDCardCfg.byName).TrimEnd('\0') +
                "，刷卡时间：" + strTime;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ID_INFO_ALARM", 0);
        }

        private void ProcessCommAlarm_AIOPVideo(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_AIOP_VIDEO_HEAD struAIOPVideo = new CHCNetSDK_Win64.NET_AIOP_VIDEO_HEAD();
            uint dwSize = (uint)Marshal.SizeOf(struAIOPVideo);
            struAIOPVideo = (CHCNetSDK_Win64.NET_AIOP_VIDEO_HEAD)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_AIOP_VIDEO_HEAD));

            //报警设备struAIOPPic地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //报警时间：年月日时分秒
            string strTimeYear = (struAIOPVideo.struTime.wYear).ToString();
            string strTimeMonth = (struAIOPVideo.struTime.wMonth).ToString("d2");
            string strTimeDay = (struAIOPVideo.struTime.wDay).ToString("d2");
            string strTimeHour = (struAIOPVideo.struTime.wHour).ToString("d2");
            string strTimeMinute = (struAIOPVideo.struTime.wMinute).ToString("d2");
            string strTimeSecond = (struAIOPVideo.struTime.wSecond).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "AI开放平台视频检测报警上传，szTaskID：" + System.Text.Encoding.UTF8.GetString(struAIOPVideo.szTaskID).TrimEnd('\0')
                + ",报警触发时间：" + strTime;

            //保存AIOPData数据  
            if ((struAIOPVideo.dwAIOPDataSize != 0) && (struAIOPVideo.pBufferAIOPData != IntPtr.Zero))
            {
                //string str = ".\\picture\\AiopData[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                //     iFileNumber + ".txt";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struAIOPVideo.dwAIOPDataSize;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struAIOPVideo.pBufferAIOPData, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("AiopData[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                    iFileNumber + ".txt", (int)struAIOPVideo.dwAIOPDataSize, struAIOPVideo.pBufferAIOPData);
                iFileNumber++;
            }
            //保存图片数据
            if ((struAIOPVideo.dwPictureSize != 0) && (struAIOPVideo.pBufferPicture != IntPtr.Zero))
            {
                //string strPic = ".\\picture\\AiopPicture[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                //     iFileNumber + ".jpg";
                //FileStream fsPic = new FileStream(strPic, FileMode.Create);
                //int iPicLen = (int)struAIOPVideo.dwPictureSize;
                //byte[] byPic = new byte[iPicLen];
                //Marshal.Copy(struAIOPVideo.pBufferPicture, byPic, 0, iPicLen);
                //fsPic.Write(byPic, 0, iPicLen);
                //fsPic.Close();
                SaveAlarmPic("AiopPicture[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                     iFileNumber + ".jpg", (int)struAIOPVideo.dwPictureSize, struAIOPVideo.pBufferPicture);
                iFileNumber++;
            }
            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_UPLOAD_AIOP_VIDEO", 0);
        }
        private void ProcessCommAlarm_AIOPPicture(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_AIOP_PICTURE_HEAD struAIOPPic = new CHCNetSDK_Win64.NET_AIOP_PICTURE_HEAD();
            uint dwSize = (uint)Marshal.SizeOf(struAIOPPic);
            struAIOPPic = (CHCNetSDK_Win64.NET_AIOP_PICTURE_HEAD)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_AIOP_PICTURE_HEAD));

            //报警设备struAIOPPic地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //报警时间：年月日时分秒
            string strTimeYear = (struAIOPPic.struTime.wYear).ToString();
            string strTimeMonth = (struAIOPPic.struTime.wMonth).ToString("d2");
            string strTimeDay = (struAIOPPic.struTime.wDay).ToString("d2");
            string strTimeHour = (struAIOPPic.struTime.wHour).ToString("d2");
            string strTimeMinute = (struAIOPPic.struTime.wMinute).ToString("d2");
            string strTimeSecond = (struAIOPPic.struTime.wSecond).ToString("d2");
            string strTime = strTimeYear + "-" + strTimeMonth + "-" + strTimeDay + " " + strTimeHour + ":" + strTimeMinute + ":" + strTimeSecond;

            string stringAlarm = "AI开放平台图片检测报警上传，szPID：" + System.Text.Encoding.UTF8.GetString(struAIOPPic.szPID).TrimEnd('\0')
                + ",报警触发时间：" + strTime;

            //保存AIOPData数据  
            if ((struAIOPPic.dwAIOPDataSize != 0) && (struAIOPPic.pBufferAIOPData != IntPtr.Zero))
            {
                //string str = ".\\picture\\AiopData[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                //     iFileNumber + ".txt";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struAIOPPic.dwAIOPDataSize;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struAIOPPic.pBufferAIOPData, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic("AiopData[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]" +
                     iFileNumber + ".txt", (int)struAIOPPic.dwAIOPDataSize, struAIOPPic.pBufferAIOPData);
                iFileNumber++;
            }
            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_UPLOAD_AIOP_PICTURE", 1);
        }
        private void ProcessCommAlarm_ISAPIAlarm(ref CHCNetSDK_Win64.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_INFO struISAPIAlarm = new CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_INFO();
            uint dwSize = (uint)Marshal.SizeOf(struISAPIAlarm);
            struISAPIAlarm = (CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_INFO)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_INFO));

            //报警设备IP地址
            string strIP = System.Text.Encoding.UTF8.GetString(pAlarmer.sDeviceIP).TrimEnd('\0');

            //保存XML或者Json数据
            string str = "";
            if ((struISAPIAlarm.dwAlarmDataLen != 0) && (struISAPIAlarm.pAlarmData != IntPtr.Zero))
            {
                if (struISAPIAlarm.byDataType == 1) // 0-invalid,1-xml,2-json
                {
                    //str = ".\\picture\\ISAPI_Alarm_XmlData_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".xml";
                    str = "ISAPI_Alarm_XmlData_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".xml";
                }
                if (struISAPIAlarm.byDataType == 2) // 0-invalid,1-xml,2-json
                {
                    //str = ".\\picture\\ISAPI_Alarm_JsonData_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".json";
                    str = "ISAPI_Alarm_JsonData_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_" + iFileNumber + ".json";
                }

                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)struISAPIAlarm.dwAlarmDataLen;
                //byte[] by = new byte[iLen];
                //Marshal.Copy(struISAPIAlarm.pAlarmData, by, 0, iLen);
                //fs.Write(by, 0, iLen);
                //fs.Close();
                SaveAlarmPic(str, (int)struISAPIAlarm.dwAlarmDataLen, struISAPIAlarm.pAlarmData);
                iFileNumber++;
            }



            for (int i = 0; i < struISAPIAlarm.byPicturesNumber; i++)
            {
                CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_PICDATA struPicData = new CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_PICDATA();
                struPicData.szFilename = new byte[256];
                Int32 nSize = Marshal.SizeOf(struPicData);
                struPicData = (CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_PICDATA)Marshal.PtrToStructure((IntPtr)((Int32)(struISAPIAlarm.pPicPackData) + i * nSize), typeof(CHCNetSDK_Win64.NET_DVR_ALARM_ISAPI_PICDATA));

                //保存图片数据
                if ((struPicData.dwPicLen != 0) && (struPicData.pPicData != IntPtr.Zero))
                {
                    //str = ".\\picture\\ISAPI_Alarm_Pic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_"
                    //     + "_" + iFileNumber + ".jpg";

                    //FileStream fs = new FileStream(str, FileMode.Create);
                    //int iLen = (int)struPicData.dwPicLen;
                    //byte[] by = new byte[iLen];
                    //Marshal.Copy(struPicData.pPicData, by, 0, iLen);
                    //fs.Write(by, 0, iLen);
                    //fs.Close();
                    SaveAlarmPic("ISAPI_Alarm_Pic_[" + strIP + "]_lUerID_[" + pAlarmer.lUserID + "]_"
                         + "_" + iFileNumber + ".jpg", (int)struPicData.dwPicLen, struPicData.pPicData);
                    iFileNumber++;
                }
            }

            string stringAlarm = "ISAPI报警信息，byDataType：" + struISAPIAlarm.byDataType + "，图片张数：" + struISAPIAlarm.byPicturesNumber;

            //if (InvokeRequired)
            //{
            //    object[] paras = new object[3];
            //    paras[0] = DateTime.Now.ToString(); //当前PC系统时间
            //    paras[1] = strIP;
            //    paras[2] = stringAlarm;
            //    listViewAlarmInfo.BeginInvoke(new UpdateListBoxCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            //}
            if (ReturnAlarm != null)
                ReturnAlarm(strIP, stringAlarm, "COMM_ISAPI_ALARM", 0);
        }
    }
}
