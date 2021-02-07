using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Model
{
    public class IdentityClientConfig
    {
        public string Scheme { get; set; }
        public string Authority { get; set; }
        public bool RequireHttpsMetadata { get; set; }
        public string ApiName { get; set; }
        public string[] CorsWithOrigins { get; set; }

    }
}
