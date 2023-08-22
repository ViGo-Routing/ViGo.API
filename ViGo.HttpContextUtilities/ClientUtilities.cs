using Microsoft.AspNetCore.Http;

namespace ViGo.HttpContextUtilities
{
    public static class ClientUtilities
    {
        public static string GetClientIpAddress(this HttpContext context)
        {
            string ipAddress = "";
            try
            {
                ipAddress = context.Connection.RemoteIpAddress.ToString();
            }
            catch (Exception ex)
            {
                ipAddress = "Invalid IP: " + ex.Message;
            }
            return ipAddress;
        }
    }
}
