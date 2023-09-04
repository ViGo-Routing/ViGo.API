using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Utilities.Configuration;
using ViGo.Utilities;

namespace ViGo.Utilities.ImageUtilities
{
    public class ReadTextRequest
    {
        public string ImageUrl { get; set; }
        public OcrType OcrType { get; set; }
    }

    public enum OcrType
    {
        ID = 1,
        DRIVER_LICENSE = 2
    }

    public static class OcrUtilities
    {
        public static async Task<ILicenseFromImage?> ReadTextFromImageAsync(
            string imageUrl, OcrType ocrType, CancellationToken cancellationToken = default)
        {
            using var client = new HttpClient();
            if (ocrType == OcrType.ID)
            {
                client.BaseAddress = new Uri("https://api.fpt.ai/vision/idr/vnm/");
            }
            else if (ocrType == OcrType.DRIVER_LICENSE)
            {
                client.BaseAddress = new Uri("https://api.fpt.ai/vision/dlr/vnm");

            }

            client.DefaultRequestHeaders.Add("api-key", ViGoConfiguration.FptAiApiKey);

            using var content = new MultipartFormDataContent("ReadText-----" +
                DateTime.Now.ToString(CultureInfo.InvariantCulture));
            Stream image = await HttpClientUtilities.GetImageFromUrlAsync(imageUrl);
            var imageStreamContent = new StreamContent(image);
            imageStreamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(imageStreamContent, "image", "read-text.png");

            var message = await client.PostAsync("", content, cancellationToken);
            if (message.IsSuccessStatusCode)
            {
                var result = await message.Content.ReadAsStringAsync(cancellationToken);
                dynamic returnResult = JObject.Parse(result);

                if (returnResult.errorCode != 0)
                {
                    throw new ApplicationException(returnResult.errorMessage);
                }

                var idData = returnResult.data[0];

                if (ocrType == OcrType.ID)
                {
                    IdFromImage idFromImage = new IdFromImage()
                    {
                        IdNumber = idData.id,
                        Name = StringUtilities.TransformToTitleCase(idData.name.ToString()),
                        Dob = idData.dob.ToString(),
                        Sex = idData.sex.ToString().ToLower() == "nam" ? true : false,
                        Address = StringUtilities.TransformToTitleCase(idData.address.ToString())
                    };
                    return idFromImage;

                }
                else if (ocrType == OcrType.DRIVER_LICENSE)
                {
                    DriverLicenseFromImage driverLicense = new DriverLicenseFromImage()
                    {
                        IdNumber = idData.id,
                        Name = StringUtilities.TransformToTitleCase(idData.name.ToString()),
                        Dob = idData.dob.ToString(),
                        Address = StringUtilities.TransformToTitleCase(idData.address.ToString())

                    };

                    return driverLicense;
                }
                return null;

            }
            else
            {
                throw new Exception("Có lỗi xảy ra khi đọc hình ảnh!!");
            }
        }
    }

    public interface ILicenseFromImage
    {
        string IdNumber { get; set; }
        string Name { get; set; }
        string Dob { get; set; }
        string Address { get; set; }
    }
    public class IdFromImage : ILicenseFromImage
    {
        public string IdNumber { get; set; }
        public string Name { get; set; }
        public string Dob { get; set; }
        public bool Sex { get; set; }
        public string Address { get; set; }
    }

    public class DriverLicenseFromImage : ILicenseFromImage
    {
        public string IdNumber { get; set; }
        public string Name { get; set; }
        public string Dob { get; set;}
        public string Address { get; set; }

    }
}
