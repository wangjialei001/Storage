using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.DBModel
{
    public class UserMapClientEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string ClientId { get; set; }
        public short ClientType { get; set; }
    }
}
