using GoogleMapsApi.Entities.DistanceMatrix.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Models.GoogleMaps;
using ViGo.Utilities.Configuration;

namespace ViGo.Utilities.Google
{
    public static class GoogleMapsApiUtilities
    {
        private static string baseUrl = "https://maps.googleapis.com/maps/api";
        
        /// <summary>
        /// In meter
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<int> GetDistanceBetweenTwoPointsAsync(GoogleMapPoint origin,
            GoogleMapPoint destination, CancellationToken cancellationToken)
        {
            IEnumerable<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("destinations", destination.ToString()),
                new KeyValuePair<string, string>("origins", origin.ToString()),
                new KeyValuePair<string, string>("key", ViGoConfiguration.GoogleMapsApi)
            };

            DistanceMatrixResponse? response = await HttpClientUtilities.SendRequestAsync
                <DistanceMatrixResponse, object>(baseUrl + "/distancematrix/json", HttpMethod.Get,
                parameters, cancellationToken: cancellationToken);
            if (response != null)
            {
                Row? firstRow = response.Rows.FirstOrDefault();
                if (firstRow == null)
                {
                    throw new Exception("No data for Distance!!");
                }
                Element? firstElement = firstRow.Elements.FirstOrDefault();
                if (firstElement == null)
                {
                    throw new Exception("No data for Distance!!");
                }
                return firstElement.Distance.Value;
            }
            return 0;
        }
    }
     
    
}
