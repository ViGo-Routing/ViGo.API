using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.HttpContextUtilities
{
    public static class ServerUtilities
    {
        public static string GetBaseUri(this HttpContext context)
        {
            string baseUri = $"{context.Request.Scheme}://" +
                $"{context.Request.Host.Value}";
            return baseUri;
        }

        public static string GetApiBaseUrl(this HttpContext context)
        {
            string apiUrl = $"{context.Request.Scheme}://" +
                $"{context.Request.Host.Value}/api";
            return apiUrl;
        }
    }
}
