using Microsoft.Extensions.Configuration;
using ViGo.Utilities.Configuration;

namespace ViGoConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Configure();
            //Console.WriteLine("admin".Encrypt());
            //string imageUrl = "https://firebasestorage.googleapis.com/v0/b/vigo-a7754.appspot.com/o/images%2Fdriver_CCCD_Front_6x9tBuGAfO1690801264506.png?alt=media&token=4e7e81bc-1891-490a-ad60-5fd0fabe905e";
            //var result = await OcrUtilities.ReadTextFromImage(imageUrl);
            //Console.WriteLine(result);
        }

        static void Configure()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true);
            IConfigurationRoot configuration = builder.Build();
            ViGoConfiguration.Initialize(configuration);
        }
    }
}