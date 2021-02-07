using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.DBModel;
using WeiCloudStorageAPI.Model;

namespace WeiCloudStorageAPI.Services
{
    public interface IAppPackageService
    {
        Task<AppPackagesModel> GetNewestAppPackage(int terminalType, int upgradeType = 0);
        Task<AppPackagesEntity> StatisticsDownloadCount(string name, long ts = 0);
    }
}
