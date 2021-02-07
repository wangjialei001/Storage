using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Model
{
    public class PostStreamModel
    {
        public string FileName { get; set; }
        public Stream Stream { get; set; }
    }
}
