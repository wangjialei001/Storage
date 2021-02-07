using System;
using System.Collections.Generic;
using System.Text;

namespace Msg.Core.UniPush
{
    public class MsgPushEntity
    {
        public List<string> ClientIds { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        /// <summary>
        /// intent：打开应用内特定页面 4
        /// url：打开网页地址 3
        /// payload：启动应用加自定义消息内容 2
        /// startapp：打开应用首页 1
        /// none：纯通知，无后续动作 0
        /// </summary>
        public int ClickType { get; set; }
        //public string Url { get; set; }
        public List<string> ToDo { get; set; }
        public string PlayLoad { get; set; }
    }
    public class MsgPushModel
    {
        public string ClientId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        /// <summary>
        /// 扩展信息
        /// </summary>
        public object PlayLoad { get; set; }
        public int Type { get; set; }
        /// <summary>
        /// 消息Id
        /// </summary>
        public long MsgId { get; set; }
    }
}
