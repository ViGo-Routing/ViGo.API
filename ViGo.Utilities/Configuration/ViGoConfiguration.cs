using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.HttpContextUtilities;

namespace ViGo.Utilities.Configuration
{
    public static class ViGoConfiguration
    {
        #region Private Members to get configuration
        private static IConfiguration? configuration;

        private static IConfiguration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    throw new Exception("Configuration has not been initialized yet!");
                }
                return configuration;
            }
        }

        #endregion

        #region Initialize the configuration

        public static void Initialize(IConfiguration _configuration)
        {
            configuration = _configuration;
        }

        #endregion

        #region Public Configuration Fields

        public static string ConnectionString(string connectionStringKey)
            => Configuration.GetConnectionString(connectionStringKey);

        public static string ValidAudience
            => Configuration.GetSection("JWT")["ValidAudience"];

        public static string ValidIssuer
            => Configuration.GetSection("JWT")["ValidIssuer"];

        public static string Secret
            => Configuration.GetSection("JWT")["Secret"];

        #endregion

        #region Security Properties
        public static string SecurityPassPhrase
            => Configuration["Security:PassPhrase"];

        public static string SecuritySalt
            => Configuration["Security:Salt"];

        public static string SecurityAlgorithm
            => Configuration["Security:Algorithm"];

        public static int SecurityPasswordIterations
            => int.Parse(Configuration["Security:PasswordIterations"]);

        public static string SecurityInitVector
            => Configuration["Security:InitVector"];

        public static int SecurityKeySize
            => int.Parse(Configuration["Security:KeySize"]);
        #endregion

        #region Firebase
        public static string FirebaseCredentialFile
            => Configuration["Google:Firebase:CredentialFile"];

        public static string FirebaseProjectId
            => Configuration["Google:Firebase:ProjectId"];

        public static string FirebaseApiKey
            => Configuration["Google:Firebase:ApiKey"];
        #endregion

        #region Google Maps
        public static string GoogleMapsApi
            => Configuration["Google:Maps:ApiKey"];
        #endregion

        #region VnPay
        public static string VnPayApiVersion
            => Configuration["Payments:VnPay:ApiVersion"];

        public static string VnPayReturnUrl(HttpContext context)
            => context.GetApiBaseUrl() +
            Configuration["Payments:VnPay:ReturnUrl"];

        public static string VnPayPaymentUrl
            => Configuration["Payments:VnPay:Url"];

        public static string VnPayQueryUrl
            => Configuration["Payments:VnPay:QueryUrl"];

        public static string VnPayTmnCode
            => Configuration["Payments:VnPay:TmnCode"];

        public static string VnPayHashSecret
            => Configuration["Payments:VnPay:SecretKey"];
        #endregion

        #region ZaloPay
        public static string ZaloPayApiUrl
            => Configuration["Payments:ZaloPay:Url"];

        public static int ZaloPayAppId
            => int.Parse(Configuration["Payments:ZaloPay:Appid"]);

        public static string ZaloPayKey1
            => Configuration["Payments:ZaloPay:Key1"];

        public static string ZaloPayKey2
            => Configuration["Payments:ZaloPay:Key2"];

        public static string ZaloPayCallback(HttpContext context)
            => context.GetApiBaseUrl() +
            Configuration["Payments:ZaloPay:ReturnUrl"];
        #endregion

        #region Background Task
        public static int QueueCapacity
            => int.Parse(Configuration["BackgroundTask:QueueCapacity"]);

        public static IConfigurationSection QuartzConfiguration
            => Configuration.GetSection("Quartz");
        #endregion

    }
}
