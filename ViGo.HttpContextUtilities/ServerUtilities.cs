using Microsoft.AspNetCore.Http;

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
