using System;
using System.Collections.Generic;
using System.Text;

namespace Msg.Core.Model
{
    public class PostClientInfoModel
    {
        public string ClientId { get; set; }
        public long UserId { get; set; }
    }
    public class UserMapClient
    {
        public string ClientId { get; set; }
        public long UserId { get; set; }
        public short ClientType { get; set; }
    }
}
