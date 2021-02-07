using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.DBModel
{
    public class SendUserMsgEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long MsgId { get; set; }
        public bool IsSend { get; set; }
        public short SendType { get; set; }
        public short IsRead { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
