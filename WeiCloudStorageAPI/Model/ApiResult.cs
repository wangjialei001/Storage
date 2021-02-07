using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Model
{
    public class ApiResult<T>
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public T Data { get; set; }
    }
}
