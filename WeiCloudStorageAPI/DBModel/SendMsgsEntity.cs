using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.DBModel
{
    public class SendMsgsEntity
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Payload { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
