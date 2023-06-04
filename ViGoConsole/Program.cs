using Microsoft.Extensions.Configuration;
using ViGo.Utilities;
using ViGo.Utilities.Configuration;

namespace ViGoConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Configure();
            Console.WriteLine("admin".Encrypt());
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