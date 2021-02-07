using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Model
{
    public class AppVersionInfoModel
    {
        public bool Update { get; internal set; }
        public string WgtUrl { get; set; }
        public string PkgUrl { get; set; }
        public string Note { get; set; }
        public short Status { get; set; }
        public string Version { get; internal set; }
        public short TerminalType { get; internal set; }
    }
}
