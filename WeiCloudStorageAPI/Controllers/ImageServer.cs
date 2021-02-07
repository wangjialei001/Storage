using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WeiCloudStorageAPI.Model;
using WeiCloudStorageAPI.Services;
using WeiCloudStorageAPI.Util;

namespace WeiCloudStorageAPI.Controllers
{
    [Route("imageserve")]
    [ApiController]
    public class ImageServer : ControllerBase
    {
        private readonly IConfiguration configuration;
        private StringCache _stringCache;
        private IQRCode _qRCode;
        private readonly IAppPackageService _appPackageService;
        public ImageServer(IConfiguration _configuration, StringCache stringCache, IQRCode qRCode, IAppPackageService appPackageService)
        {
            configuration = _configuration;
            _stringCache = stringCache;
            _qRCode = qRCode;
            _appPackageService = appPackageService;
        }

        [HttpGet("{path}/{fileName}")]
        public async Task<IActionResult> Get(string path, string fileName)
        {
            return await GetObj(path, fileName);
        }
        private async Task<IActionResult> GetObj(string path, string fileName)
        {
            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                var bucketName = "imageserve";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                var ms = new MemoryStream();
                //using (var ms = new MemoryStream())
                {
                    //var filePath = "";
                    //var paths = path.Split("/").ToList();
                    //foreach(var pt in paths)
                    //{
                    //    filePath += Path.Combine(filePath, pt);
                    //}
                    //fileName = Path.Combine(filePath, fileName);
                    var paths = path.Split(",").ToList();
                    fileName = string.Join("/", paths) + "/" + fileName;

                    await minioClient.GetObjectAsync(bucketName, fileName, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    //
                    string extend = fileName.Substring(fileName.LastIndexOf(".") + 1);

                    Console.WriteLine("下载文件：" + fileName);

                    //jpg、png、jpeg、bmp、gif
                    if (extend.Equals("jpg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("png", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("bmp", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var bytes = ms.GetBuffer();
                        return new FileContentResult(bytes, "image/jpeg");
                        //return File(ms, "image/jpeg", fileName);
                    }
                    else
                    {
                        ms.Flush();
                        ms.Position = 0;
                        //return new FileContentResult(bytes, "application/octet-stream");
                        return File(ms, "application/octet-stream", fileName);
                    }
                }
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return NoContent();
        }
        [HttpGet("{fileName}")]
        //[HttpGet]
        public async Task<IActionResult> Get(string fileName)
        {
            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                var bucketName = "imageserve";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                var ms = new MemoryStream();
                //using (var ms = new MemoryStream())
                {
                    await minioClient.GetObjectAsync(bucketName, fileName, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    //
                    string extend = fileName.Substring(fileName.LastIndexOf(".") + 1);

                    Console.WriteLine("下载文件：" + fileName);

                    //jpg、png、jpeg、bmp、gif
                    if (extend.Equals("jpg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("png", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("bmp", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var bytes = ms.GetBuffer();
                        return new FileContentResult(bytes, "image/jpeg");
                        //return File(ms, "image/jpeg", fileName);
                    }
                    else
                    {
                        ms.Flush();
                        ms.Position = 0;
                        //return new FileContentResult(bytes, "application/octet-stream");
                        return File(ms, "application/octet-stream", fileName);
                    }
                }
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return NoContent();
        }
        [Authorize]
        [HttpGet("getobj/{fileName}")]
        public async Task<ApiResult<string>> GetImageBase64(string fileName)
        {
            var result = new ApiResult<string> { Code = RequestBackStatuEnum.success.Value };

            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                string extend = fileName.Substring(fileName.LastIndexOf(".") + 1);

                Console.WriteLine("下载文件：" + fileName);
                //jpg、png、jpeg、bmp、gif
                if (!extend.Equals("jpg", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("png", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("bmp", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Code = RequestBackStatuEnum.badrequest.Value;
                    result.Msg = "jpg、png、jpeg、bmp、gif格式不正确";
                    return result;
                }
                var bucketName = "imageserve";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                using (var ms = new MemoryStream())
                {
                    await minioClient.GetObjectAsync(bucketName, fileName, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    var bytes = ms.GetBuffer();
                    result.Data = Convert.ToBase64String(bytes);
                }
            }
            catch (MinioException e)
            {
                result.Code = RequestBackStatuEnum.fail.Value;
                result.Msg = e.Message;
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return result;
        }
        [Authorize]
        [HttpGet("getobj/{width}/{fileName}")]
        public async Task<ApiResult<string>> GetImageBase64(int width, string fileName)
        {
            var result = new ApiResult<string> { Code = RequestBackStatuEnum.success.Value };

            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                string extend = fileName.Substring(fileName.LastIndexOf(".") + 1);

                Console.WriteLine("下载文件：" + fileName);
                //jpg、png、jpeg、bmp、gif
                if (!extend.Equals("jpg", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("png", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("bmp", StringComparison.InvariantCultureIgnoreCase)
                    && !extend.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Code = RequestBackStatuEnum.badrequest.Value;
                    result.Msg = "jpg、png、jpeg、bmp、gif格式不正确";
                    return result;
                }
                var bucketName = "imageserve";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);

                var obj = await minioClient.StatObjectAsync(bucketName, fileName);

                var tick = obj.LastModified.Ticks;
                string cacheName = "Base64_" + width + "_" + fileName + "_" + tick;
                var r = _stringCache.GetValue<string>(cacheName);
                if (!string.IsNullOrEmpty(r))
                {
                    result.Data = r;
                    return result;
                }

                using (var ms = new MemoryStream())
                {
                    await minioClient.GetObjectAsync(bucketName, fileName, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    ms.Seek(0, SeekOrigin.Begin);
                    var image = SixLabors.ImageSharp.Image.Load(ms, out IImageFormat format);
                    var height = width * image.Height / image.Width;
                    var imgNew = image.Clone(t => t.Resize(width, height));
                    result.Data = imgNew.ToBase64String(format);
                    var base64ExpireStr = configuration["Base64Expire"];
                    var base64Expire = 3600;
                    if (!int.TryParse(base64ExpireStr, out base64Expire))
                        base64Expire = 3600;
                    _stringCache.SetValue<string>(cacheName, result.Data, base64Expire);
                }
            }
            catch (MinioException e)
            {
                result.Code = RequestBackStatuEnum.fail.Value;
                result.Msg = e.Message;
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return result;
        }

        [Authorize]
        [HttpGet("file/{fileName}")]
        public async Task<IActionResult> GetFile(string fileName)
        {
            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                var bucketName = "imageserve";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                var ms = new MemoryStream();
                //using (var ms = new MemoryStream())
                {
                    await minioClient.GetObjectAsync(bucketName, fileName, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    //
                    string extend = fileName.Substring(fileName.LastIndexOf(".") + 1);

                    Console.WriteLine("下载文件：" + fileName);

                    //jpg、png、jpeg、bmp、gif
                    if (extend.Equals("jpg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("png", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("bmp", StringComparison.InvariantCultureIgnoreCase)
                        || extend.Equals("gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var bytes = ms.GetBuffer();
                        return new FileContentResult(bytes, "image/jpeg");
                        //return File(ms, "image/jpeg", fileName);
                    }
                    else
                    {
                        ms.Flush();
                        ms.Position = 0;
                        //return new FileContentResult(bytes, "application/octet-stream");
                        return File(ms, "application/octet-stream", fileName);
                    }
                }
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return NoContent();
        }

        [HttpPost]
        [Route("file")]
        public async Task<string> Post([FromForm] IFormFile file)
        {
            string fileName = file.FileName;
            MemoryStream stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return await Post(stream, fileName);
        }
        [HttpPost]
        [Route("file1")]
        public async Task<string> Post([FromForm] IFormFile file,[FromQuery]string bucket, [FromQuery] string paths)
        {
            string fileName = file.FileName;
            MemoryStream stream = new MemoryStream();
            await file.CopyToAsync(stream);
            if (!string.IsNullOrEmpty(paths))
            {
                return await UploadFile(stream, fileName, bucket, paths.Split(",").ToList());
            }
            return await UploadFile(stream, fileName, bucket);
        }
        private async Task<string> UploadFile(Stream stream, string fileName, string bucket = "", List<string> paths = null)
        {
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                var endpoint = configuration["FileServer:Url"];
                var accessKey = configuration["FileServer:AccessKey"];
                var secretKey = configuration["FileServer:SecretKey"];
                var bucketName = "imageserve";
                if (!string.IsNullOrEmpty(bucket))
                {
                    bucketName = bucket;
                }
                if (paths != null && paths.Count() > 0)
                {
                    var _fileName = "";
                    foreach (var path in paths)
                    {
                        _fileName = Path.Combine(_fileName, path);
                    }
                    _fileName = Path.Combine(_fileName, fileName);
                    fileName = _fileName;
                }
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                await minioClient.PutObjectAsync(bucketName, fileName, stream, stream.Length);
                Console.WriteLine("上传文件：" + fileName);
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
                return "error:" + e.Message;
            }
            return fileName;
        }
        [HttpPost]
        [Route("stream")]
        public async Task<string> Post([FromForm] Stream stream, [FromQuery] string fileName)
        {
            return await UploadFile(stream, fileName);
        }
        [HttpGet]
        [Route("GetAppVersionInfo")]
        public async Task<AppVersionInfoModel> GetAppVersionInfo(string name, string version, int terminalType)
        {
            //var configuration = new ConfigurationBuilder()
            //    .AddJsonFile("config.json", optional: false, reloadOnChange: false)
            //    .AddEnvironmentVariables()
            //    .Build();
            //var appName = configuration["appInfo:name"];
            //var appVersion = configuration["appInfo:version"];
            //var result = new AppVersionInfoModel { };
            //if (string.Equals(version, appVersion, StringComparison.OrdinalIgnoreCase))
            //{//不需要更新
            //    result.Update = false;
            //}
            //else
            //{
            //    result.Update = true;
            //    result.WgtUrl = configuration["appInfo:wgtUrl"];
            //    result.PkgUrl = configuration["appInfo:pkgUrl"];
            //    result.Note = configuration["appInfo:note"];
            //    var statusStr = configuration["appInfo:status"];
            //    short status = 0;
            //    if (short.TryParse(statusStr, out status))
            //    {
            //        result.Status = status;
            //    }
            //}
            //return await Task.FromResult(result);

            if (terminalType == 0)
            {
                terminalType = 1;//默认android
            }
            var appPackage = await _appPackageService.GetNewestAppPackage(terminalType);

            var result = new AppVersionInfoModel { };
            if (string.Equals(version, appPackage.Version, StringComparison.OrdinalIgnoreCase))
            {//不需要更新
                result.Update = false;
            }
            else
            {
                result.Update = true;
                if (appPackage.UpgradeType == 1)
                {//全包
                    result.PkgUrl = configuration["FileUpload:loadUrl"] + "DownAppPackage/" + appPackage.PackageUrl;
                }
                else if (appPackage.UpgradeType == 2)
                {
                    result.WgtUrl = configuration["FileUpload:loadUrl"] + "DownAppPackage/" + appPackage.PackageUrl;
                    //result.WgtUrl = "http://www.weienergy.cn/__UNI__5CD0F32.wgt";
                }
                result.Note = appPackage.Content;
                //var statusStr = configuration["appInfo:status"];
                //short status = 0;
                //if (short.TryParse(statusStr, out status))
                //{
                //    result.Status = status;
                //}
            }
            result.Version = appPackage.Version;
            result.TerminalType = appPackage.TerminalType;
            return await Task.FromResult(result);
        }

        /// <summary>
        /// app包下载
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("DownAppPackage/{name}")]
        //[Route("DownAppPackage")]
        public async Task<IActionResult> DownAppPackage(string name)
        {
            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                var bucketName = "app";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                var ms = new MemoryStream();
                //using (var ms = new MemoryStream())
                {
                    await minioClient.GetObjectAsync(bucketName, name, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    ms.Flush();
                    ms.Position = 0;
                    await _appPackageService.StatisticsDownloadCount(name);
                    //return File(ms, "application/vnd.android.package-archive", name);
                    return File(ms, "application/octet-stream", name);
                }
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return NoContent();
        }
        /// <summary>
        /// 下载最新包
        /// </summary>
        /// <param name="terminalType">1-android,2-ios</param>
        /// <returns></returns>
        [HttpGet("DownNewestAppPackage")]
        public async Task<IActionResult> DownNewestAppPackage(int terminalType,long ts)
        {
            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                if (terminalType==0)
                {
                    terminalType = 1;//默认android
                }
                var appPackage = await _appPackageService.GetNewestAppPackage(terminalType, 1);

                var bucketName = "app";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                // Check whether the object exists using statObject().
                // If the object is not found, statObject() throws an exception,
                // else it means that the object exists.
                // Execution is successful.
                var ms = new MemoryStream();
                //using (var ms = new MemoryStream())
                {
                    await minioClient.GetObjectAsync(bucketName, appPackage.PackageUrl, (stream) =>
                    {
                        stream.CopyTo(ms);
                    });
                    ms.Flush();
                    ms.Position = 0;
                    await _appPackageService.StatisticsDownloadCount(appPackage.PackageUrl, ts);
                    //return File(ms, "application/vnd.android.package-archive", name);
                    return File(ms, "application/octet-stream", appPackage.PackageUrl);
                }
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return NoContent();
        }

        [HttpGet]
        [Route("UploadAppPackage")]
        public async Task<IActionResult> UploadAppPackage([FromForm] IFormFile file)
        {
            var endpoint = configuration["FileServer:Url"];
            var accessKey = configuration["FileServer:AccessKey"];
            var secretKey = configuration["FileServer:SecretKey"];
            try
            {
                string fileName = file.FileName;
                MemoryStream stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var bucketName = "app";
                var minioClient = new MinioClient(endpoint, accessKey, secretKey);
                await minioClient.PutObjectAsync(bucketName, fileName, stream, stream.Length);
                Console.WriteLine("上传文件：" + fileName);
            }
            catch (MinioException e)
            {
                Console.Out.WriteLine("Error occurred: " + e);
            }
            return NoContent();
        }
        [HttpPost]
        [Route("QrCode")]
        public FileContentResult QrCode([FromBody] QrCodeModel input)
        {
            var buffer = _qRCode.GenerateQRCode(input.Content);
            return File(buffer, "image/jpeg");
        }
    }
}
