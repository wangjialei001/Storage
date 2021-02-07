using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Util
{
    public interface IQRCode
    {
        byte[] GenerateQRCode(string content);
    }

    public class QRCode : IQRCode
    {
        public byte[] GenerateQRCode(string content)
        {
            var generator = new QRCodeGenerator();

            var codeData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M, true);
            QRCoder.QRCode qrcode = new QRCoder.QRCode(codeData);
            var path = Path.Combine(AppContext.BaseDirectory,"Images","1.png");
            Bitmap icon = new Bitmap(path);
            var bitmapImg = qrcode.GetGraphic(10, Color.Black, Color.White, icon: icon, drawQuietZones: false);

            using (MemoryStream stream = new MemoryStream())
            {
                bitmapImg.Save(stream, ImageFormat.Jpeg);
                return stream.GetBuffer();
            }
        }
    }
}
