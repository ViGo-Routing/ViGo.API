using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
