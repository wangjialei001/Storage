using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Msg.Core.UniPush
{
    public class UniPushUtil
    {
        private HttpClient httpClient = new HttpClient();
        private string token = string.Empty;

        private readonly string url = string.Empty;
        private readonly string appKey = string.Empty;
        private readonly string masterSecret = string.Empty;
        private readonly string appId = string.Empty;
        private readonly int TTL = 0;
        private readonly IConfiguration configuration;
        public UniPushUtil()
        {
            configuration = new ConfigurationBuilder()
                .AddJsonFile("msgCoreConfig.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            url = configuration["UniPush:Url"];
            appKey = configuration["UniPush:AppKey"];
            masterSecret = configuration["UniPush:MasterSecret"];
            appId = configuration["UniPush:AppId"];
            var TTLStr= configuration["UniPush:TTL"];
            if (!int.TryParse(TTLStr, out TTL))
                TTL = 3600000;

        }
        private HttpClient httpClientToken = new HttpClient();


        public async Task GetToken()
        {
            var bodyObj = new UniTokenReqBody
            {
                timestamp = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000).ToString(),
                appkey = appKey
            };
            bodyObj.sign = SHA256EncryptString(bodyObj.appkey + bodyObj.timestamp + masterSecret);

            var body = JsonConvert.SerializeObject(bodyObj);
            var buffer = Encoding.UTF8.GetBytes(body);
            var byteContent = new ByteArrayContent(buffer);

            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = new HttpResponseMessage();
            response = await httpClientToken.PostAsync(url + appId + "/auth", byteContent);
            var resultStr = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(resultStr))
            {
                var result = JsonConvert.DeserializeObject<UniResBaseModel<UniTokenResBody>>(resultStr);
                if (result != null && result.data != null && !string.IsNullOrEmpty(result.data.token))
                    token = result.data.token;
            }
            Console.WriteLine(resultStr);
        }
        private string SHA256EncryptString(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256Managed.Create().ComputeHash(bytes);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            return builder.ToString();
        }
        public async Task<object> Push(MsgPushEntity msgPush)
        {// "d45e0bccd9dcd6450012f9b6bb130b37"
            if (string.IsNullOrEmpty(token))
            {
                await GetToken();
            }
            var notification = new UniPushNofiyModel
            {
                title = msgPush.Title,
                body = msgPush.Body
            };
            string _clickType = string.Empty;
            if (msgPush.ClickType == 0)
            {
                _clickType = "none";
            }
            else if (msgPush.ClickType == 1)
            {
                _clickType = "startapp";
                //notification.payload = JsonConvert.SerializeObject(new { ProjectId = 1, Id = 2, Type = 1 });
                //notification.payload = "ProjectId=1234567";
            }
            else if (msgPush.ClickType == 2)
            {
                _clickType = "payload";
                notification.payload = msgPush.ToDo?.FirstOrDefault() ?? string.Empty;
                //notification.payload = JsonConvert.SerializeObject(new { ProjectId = 1, Id = 2, Type = 1 });
                //notification.payload = "ProjectId=1234567";
            }
            else if (msgPush.ClickType == 3)
            {
                _clickType = "url";
                notification.url = msgPush.ToDo?.FirstOrDefault() ?? string.Empty;
            }
            else if (msgPush.ClickType == 4)
            {
                _clickType = "intent";
                //notification.intent = $"intent:#Intent;component={configuration["UniApp:Package"]}/你要打开的 activity 全路径;S.parm1=value1;S.parm2=value2;end";// msgPush.ToDo;

                notification.intent = $"intent:#Intent;component={configuration["UniApp:Package"]}/{msgPush.ToDo.FirstOrDefault()};";// msgPush.ToDo;
                if (msgPush.ToDo.Count() > 1)
                {
                    for (var i = 1; i < msgPush.ToDo.Count(); i++)
                    {
                        notification.intent += $"S.stringType={msgPush.ToDo[i]};";
                    }
                }
                notification.intent += "end";
            }
            notification.click_type = _clickType;
            notification.channel_level = 4;
            notification.payload = "hello";
            #region 注释

            //notification.options = new List<UniPushNofiyOptionModel> {
            //        new UniPushNofiyOptionModel
            //        { constraint = "HW",
            //            key = "badgeAddNum",
            //            value = "1"
            //        },
            //        new UniPushNofiyOptionModel {
            //            constraint = "HW",
            //            key = "badgeClass",
            //            value = "com.getui.demo.GetuiSdkDemoActivity"
            //        },
            //        new UniPushNofiyOptionModel {
            //            constraint = "HW",
            //            key = "icon",
            //            value = "https://xxx"
            //        },
            //        new UniPushNofiyOptionModel {
            //            constraint = "OP",
            //            key = "app_message_id",
            //            value = "xxx"
            //        },
            //        new UniPushNofiyOptionModel {
            //            constraint = "OP",
            //            key = "channel",
            //            value = "Default"
            //        },
            //        new UniPushNofiyOptionModel {
            //            constraint = "VV",
            //            key = "classification",
            //            value = 0
            //        },
            //        new UniPushNofiyOptionModel {
            //            constraint = "XM",
            //            key = "channel",
            //            value = "xxx"
            //        }
            //    };
            #endregion

            var bodyObj = new UniPushModel
            {
                is_async = true,
                msg_list = new List<UniPushMsgModel>
                {
                    
                }
            };
            foreach (var clientId in msgPush.ClientIds)
            {
                var msg = new UniPushMsgModel
                {
                    request_id = Guid.NewGuid().ToString("N"),
                    settings = new UniPushMsgSettingModel { ttl = TTL },
                    audience = new UniPushAudienceModel { cid = new List<string> { clientId } },
                    push_message = new UniPushMessageModel
                    {
                        notification = notification
                    }
                };
                bodyObj.msg_list.Add(msg);
            }
            var body = JsonConvert.SerializeObject(bodyObj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var buffer = Encoding.UTF8.GetBytes(body);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.Add("token", token);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync(url + appId + "/push/single/batch/cid", byteContent);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new { Code = 200 };
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await GetToken();
                byteContent.Headers.Remove("token");
                byteContent.Headers.Add("token", token);
                response = await httpClient.PostAsync(url + appId + "/push/single/batch/cid", byteContent);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new { Code = 200 };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new { Code = 401 };
                }
            }
            var resultStr = await response.Content.ReadAsStringAsync();
            string resultMsg = string.Empty;
            if (!string.IsNullOrEmpty(resultStr))
            {
                var result = JsonConvert.DeserializeObject<UniResBaseModel<string>>(resultStr);
                if (result != null && result.data != null)
                    resultMsg = result.msg;
            }
            return new { Code = 500, Msg = resultMsg };
        }
        /// <summary>
        /// 推送消息（循环执行单条）
        /// </summary>
        /// <param name="msgPush"></param>
        /// <returns></returns>
        public async Task<object> Push1(MsgPushEntity msgPush)
        {// "d45e0bccd9dcd6450012f9b6bb130b37"
            if (string.IsNullOrEmpty(token))
            {
                await GetToken();
            }

            var bodyObj = new UniPushMsgModel
            {
                request_id = Guid.NewGuid().ToString("N"),
                settings = new UniPushMsgSettingModel { ttl = TTL },
                audience = new UniPushAudienceModel { cid = new List<string> { msgPush.ClientIds.FirstOrDefault() } },
                push_message = new UniPushMessageModel
                {
                    //transmission = "{\"title\": \"" + msgPush.Title + "\",\"content\": \"" + msgPush.Body + "\",\"payload\": {\"ProjectId\":1}}"
                    transmission = "{\"title\": \"" + msgPush.Title + "\",\"content\": \"" + msgPush.Body + "\",\"payload\":{" + msgPush.PlayLoad + "}}"
                }
            };
            
            var body = JsonConvert.SerializeObject(bodyObj, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var buffer = Encoding.UTF8.GetBytes(body);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.Add("token", token);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync(url + appId + "/push/single/cid", byteContent);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new { Code = 200 };
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await GetToken();
                byteContent.Headers.Remove("token");
                byteContent.Headers.Add("token", token);
                response = await httpClient.PostAsync(url + appId + "/push/single/batch/cid", byteContent);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new { Code = 200 };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new { Code = 401 };
                }
            }
            var resultStr = await response.Content.ReadAsStringAsync();
            string resultMsg = string.Empty;
            if (!string.IsNullOrEmpty(resultStr))
            {
                var result = JsonConvert.DeserializeObject<UniResBaseModel<string>>(resultStr);
                if (result != null && result.data != null)
                    resultMsg = result.msg;
            }
            return new { Code = 500, Msg = resultMsg };
        }

        public async Task<object> Push11(MsgPushEntity msgPush)
        {// "d45e0bccd9dcd6450012f9b6bb130b37"
            if (string.IsNullOrEmpty(token))
            {
                await GetToken();
            }

            var bodyObj = new UniPushMsgModel
            {
                request_id = Guid.NewGuid().ToString("N"),
                settings = new UniPushMsgSettingModel { ttl = TTL },
                audience = new UniPushAudienceModel { cid = new List<string> { msgPush.ClientIds.FirstOrDefault() } },
                push_message = new UniPushMessageModel
                {
                    //transmission = "{\"title\": \"" + msgPush.Title + "\",\"content\": \"" + msgPush.Body + "\",\"payload\": {\"ProjectId\":1}}"
                    transmission = "{\"title\": \"" + msgPush.Title + "\",\"content\": \"" + msgPush.Body + "\",\"payload\":{" + msgPush.PlayLoad + "}}"
                }
            };
            string trans = "\"title\": \"" + msgPush.Title + "\",\"content\": \"" + msgPush.Body + "\",\"payload\":{" + msgPush.PlayLoad + "}";
            //var body = JsonConvert.SerializeObject(bodyObj, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var body = "{\"request_id\":\""+ Guid.NewGuid().ToString("N") + "\",\"settings\":{\"ttl\":"+ TTL + "},\"audience\":{\"cid\":[\""+ msgPush.ClientIds.FirstOrDefault() + "\"]},\"push_message\":{\"transmission\":\"{"+ trans + "}\"}}";
            var buffer = Encoding.UTF8.GetBytes(body);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.Add("token", token);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync(url + appId + "/push/single/cid", byteContent);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new { Code = 200 };
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await GetToken();
                byteContent.Headers.Remove("token");
                byteContent.Headers.Add("token", token);
                response = await httpClient.PostAsync(url + appId + "/push/single/batch/cid", byteContent);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new { Code = 200 };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new { Code = 401 };
                }
            }
            var resultStr = await response.Content.ReadAsStringAsync();
            string resultMsg = string.Empty;
            if (!string.IsNullOrEmpty(resultStr))
            {
                var result = JsonConvert.DeserializeObject<UniResBaseModel<string>>(resultStr);
                if (result != null && result.data != null)
                    resultMsg = result.msg;
            }
            return new { Code = 500, Msg = resultMsg };
        }

        public async Task<object> Push2(MsgPushEntity msgPush)
        {// "d45e0bccd9dcd6450012f9b6bb130b37"
            if (string.IsNullOrEmpty(token))
            {
                await GetToken();
            }
            var notification = new UniPushNofiyModel
            {
                title = msgPush.Title,
                body = msgPush.Body
            };
            string _clickType = string.Empty;
            if (msgPush.ClickType == 0)
            {
                _clickType = "none";
            }
            else if (msgPush.ClickType == 1)
            {
                _clickType = "startapp";
                //notification.payload = JsonConvert.SerializeObject(new { ProjectId = 1, Id = 2, Type = 1 });
                //notification.payload = "ProjectId=1234567";
            }
            else if (msgPush.ClickType == 2)
            {
                _clickType = "payload";
                notification.payload = msgPush.ToDo?.FirstOrDefault() ?? string.Empty;
                //notification.payload = JsonConvert.SerializeObject(new { ProjectId = 1, Id = 2, Type = 1 });
                //notification.payload = "ProjectId=1234567";
            }
            else if (msgPush.ClickType == 3)
            {
                _clickType = "url";
                notification.url = msgPush.ToDo?.FirstOrDefault() ?? string.Empty;
            }
            else if (msgPush.ClickType == 4)
            {
                _clickType = "intent";
                //notification.intent = $"intent:#Intent;component={configuration["UniApp:Package"]}/你要打开的 activity 全路径;S.parm1=value1;S.parm2=value2;end";// msgPush.ToDo;

                //notification.intent = $"intent:#Intent;component={configuration["UniApp:Package"]}/{msgPush.ToDo?.FirstOrDefault()??string.Empty};";// msgPush.ToDo;
                //if (msgPush.ToDo.Count() > 1)
                //{
                //    for (var i = 1; i < msgPush.ToDo.Count(); i++)
                //    {
                //        notification.intent += $"S.stringType={msgPush.ToDo[i]};";
                //    }
                //}
                notification.intent = $"intent:#Intent;component={configuration["UniApp:Package"]}/{msgPush.ToDo?.FirstOrDefault() ?? string.Empty};S.title=测试;S.content=测试内容;";
                notification.intent += "end";
            }
            notification.click_type = _clickType;
            notification.channel_level = 4;
            notification.payload = "hello";


            var bodyObj = new UniPushMsgModel
            {
                request_id = Guid.NewGuid().ToString("N"),
                settings = new UniPushMsgSettingModel { ttl = TTL },
                audience = new UniPushAudienceModel { cid = new List<string> { msgPush.ClientIds.FirstOrDefault() } },
                push_message = new UniPushMessageModel
                {
                    notification = notification
                }
            };

            var body = JsonConvert.SerializeObject(bodyObj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            var buffer = Encoding.UTF8.GetBytes(body);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.Add("token", token);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await httpClient.PostAsync(url + appId + "/push/single/cid", byteContent);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return new { Code = 200 };
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await GetToken();
                byteContent.Headers.Remove("token");
                byteContent.Headers.Add("token", token);
                response = await httpClient.PostAsync(url + appId + "/push/single/batch/cid", byteContent);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new { Code = 200 };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new { Code = 401 };
                }
            }
            var resultStr = await response.Content.ReadAsStringAsync();
            string resultMsg = string.Empty;
            if (!string.IsNullOrEmpty(resultStr))
            {
                var result = JsonConvert.DeserializeObject<UniResBaseModel<string>>(resultStr);
                if (result != null && result.data != null)
                    resultMsg = result.msg;
            }
            return new { Code = 500, Msg = resultMsg };
        }


        public async Task SendMsg()
        {
            httpClient.BaseAddress = new Uri("https://restapi.getui.com/v2/aVfYfaFq25Apol0iVhbOe1");
            httpClient.DefaultRequestHeaders.Add("authtoken", "");
            httpClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
            httpClient.DefaultRequestHeaders.Add("'content-type", "application/json");
            var msgModel = new UniMsgEntity
            {
                Message = new UniMessageEntity
                {
                    AppKey = "UdbBLH41HV5rCsqpL94DW3",
                    Is_Offline = true,
                    MsgType = "transmission"
                },
                TransMission = new UniTransMissionEntity
                {
                    TransMission_Type = false,
                    TransMission_Content = "this"
                },
                Notify = new UniNotifyEntity
                {
                    Title="消息",
                    Content= "消息内容",
                    //Intent= "intent:#Intent;action=android.intent.action.oppopush;launchFlags=0x14000000;component=io.dcloud.HBuilder/io.dcloud.PandoraEntry;S.UP-OL-SU=true;S.title=测试标题;S.content=测试内容;S.payload=test;end",
                    Type="1"
                },
                CId=string.Empty,
                RequestId=""
            };
            msgModel.Notify.Intent = $"intent:#Intent;action=android.intent.action.oppopush;launchFlags=0x14000000;component=com.weienergy.app/io.dcloud.PandoraEntry;S.UP-OL-SU=true;S.title={msgModel.Notify.Title};S.content={msgModel.Notify.Content};S.payload=test;end";

            var body = JsonConvert.SerializeObject(msgModel);
            var buffer = Encoding.UTF8.GetBytes(body);
            var byteContent = new ByteArrayContent(buffer);
            var result = await httpClient.PostAsync("/", byteContent);
        }
    }
}
