using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Model
{
    public class AppPackagesModel
    {
        public long Id { get; set; }
        public string PackageName { get; set; }
        public string Version { get; set; }
        public short TerminalType { get; set; }
        public short UpgradeType { get; set; }
        public string Content { get; set; }
        public string PackageUrl { get; set; }
        public int DownCount { get; set; }
    }
}
