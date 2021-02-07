using System;
using System.Collections.Generic;
using System.Text;

namespace Msg.Core.UniPush
{
    public class UniMsgEntity
    {
        public string CId { get; set; }
        public string RequestId { get; set; }
        public UniMessageEntity Message { get; set; }
        public UniTransMissionEntity TransMission { get; set; }
        public UniNotifyEntity Notify { get; set; }
    }
    public class UniMessageEntity
    {
        public string AppKey { get; set; }
        public bool Is_Offline { get; set; }
        public string MsgType { get; set; }
    }
    public class UniTransMissionEntity
    {
        public bool TransMission_Type { get; set; }
        public string TransMission_Content { get; set; }
    }
    public class UniNotifyEntity
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Intent { get; set; }
        public string Type { get; set; }
    }
    #region token
    public class UniTokenReqBody
    {
        public string sign { get; set; }
        public string timestamp { get; set; }
        public string appkey { get; set; }
    }
    public class UniTokenResBody
    {
        public string expire_time { get; set; }
        public string token { get; set; }
    }
    #endregion

    #region responseBaseModel
    public class UniResBaseModel<T>
    {
        public string msg { get; set; }
        public int code { get; set; }
        public T data { get; set; }
    }
    #endregion
    #region push
    public class UniPushModel
    {
        public bool is_async { get; set; }
        public List<UniPushMsgModel> msg_list { get; set; }
    }
    public class UniPushMsgModel
    {
        public string request_id { get; set; }
        public UniPushMsgSettingModel settings { get; set; }
        public UniPushAudienceModel audience { get; set; }
        public UniPushMessageModel push_message { get; set; }
    }
    public class UniPushMsgSettingModel
    {
        public int ttl { get; set; }
    }
    public class UniPushAudienceModel
    {
        public List<string> cid { get; set; }
    }
    public class UniPushMessageModel
    {
        //public UniPushTransmissionModel transmission { get; set; }
        //public UniPushNofiyModel transmission { get; set; }
        public string transmission { get; set; }

        public UniPushNofiyModel notification { get; set; }
    }
    public class UniPushTransmissionModel
    {
        public bool transmission_type { get; set; }
        public string transmission_content { get; set; }
        public UniPushNofiyModel notify { get; set; }
    }
    public class UniPushNofiyModel
    {
        public List<UniPushNofiyOptionModel> options { get; set; }
        public string payload { get; set; }
        public string intent { get; set; }

        public string title { get; set; }
        public string body { get; set; }
        public string click_type { get; set; }
        public string url { get; set; }
        public int? channel_level { get; set; }
        public string extend { get; set; }
    }
    public class UniPushNofiyOptionModel
    {
        public string constraint { get; set; }
        public string key { get; set; }
        public object value { get; set; }
    }
    #endregion
}
