using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace ViGo.Utilities
{
    public static class HttpClientUtilities
    {
        private static string GenerateParameters(
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var paramList = from param in parameters
                            select $"{HttpUtility.HtmlEncode(param.Key)}={HttpUtility.HtmlEncode(param.Value)}";
            return string.Join("&", paramList);
        }

        public static async Task<T?> SendRequestAsync<T, M>(
            this HttpClient httpClient,
            string requestUrl,
            HttpMethod method,
            IEnumerable<KeyValuePair<string, string>>? parameters = null,
            M? body = null,
            CancellationToken cancellationToken = default
            ) where T : class where M : class
        {
            T? result = null;

            //using (var client = new HttpClient())
            //{
            httpClient.BaseAddress = new Uri(requestUrl);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = new HttpResponseMessage();
                string queryString = parameters is null ? string.Empty :
                    "?" + GenerateParameters(parameters);
                string bodyJson = body is null ? string.Empty :
                    JsonConvert.SerializeObject(body);

                if (method == HttpMethod.Get)
                {
                    response = await httpClient.GetAsync(queryString);
                }
                else if (method == HttpMethod.Post)
                {
                    StringContent content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(queryString, content, cancellationToken);
                }

                string resultText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    result = JsonConvert.DeserializeObject<T>(resultText);
                }
                else
                {
                    throw new Exception("Failed to fetch: " +
                        requestUrl + queryString + "\n" +
                        "Status Code: " + response.StatusCode + "\n" +
                        "Response: " + resultText);
                }
            //}

            return result;
        }

        public static async Task<Stream> GetImageFromUrlAsync(
            this HttpClient httpClient,
            string imageUrl)
        {
            //using (var client = new HttpClient())
            //{
                using (var response = await httpClient.GetAsync(imageUrl))
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                    return new MemoryStream(imageBytes);
                }
            //}
        }
    }
}
