﻿using GoogleMapsApi.Entities.DistanceMatrix.Response;
using ViGo.Models.GoogleMaps;
using ViGo.Utilities.Configuration;

namespace ViGo.Utilities.Google
{
    public static class GoogleMapsApiUtilities
    {
        private static string baseUrl = "https://maps.googleapis.com/maps/api";

        /// <summary>
        /// In kilometers
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<double> GetDistanceBetweenTwoPointsAsync(
            GoogleMapPoint origin, GoogleMapPoint destination,
            //DateTime? departureTime,
            CancellationToken cancellationToken)
        {
            IEnumerable<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("destinations", destination.ToString()),
                new KeyValuePair<string, string>("origins", origin.ToString()),
                new KeyValuePair<string, string>("key", ViGoConfiguration.GoogleMapsApi)
            };

            //if (departureTime.HasValue)
            //{
            //    long departureTimeInLong = DateTimeUtilities.GetTimeStamp(departureTime.Value);
            //    parameters = parameters.Append(new KeyValuePair<string, string>("departure_time", departureTimeInLong.ToString()));
            //}

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
                if (firstElement == null || firstElement.Distance == null)
                {
                    throw new Exception("No data for Distance!!");
                }
                return (double)firstElement.Distance.Value / 1000; // Response Value in meters
            }
            return 0;
        }

        /// <summary>
        /// In minutes
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<double> GetDurationBetweenTwoPointsAsync(
            GoogleMapPoint origin, GoogleMapPoint destination,
            //DateTime? departureTime,
            CancellationToken cancellationToken)
        {
            IEnumerable<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("destinations", destination.ToString()),
                new KeyValuePair<string, string>("origins", origin.ToString()),
                new KeyValuePair<string, string>("key", ViGoConfiguration.GoogleMapsApi)
            };

            //if (departureTime.HasValue)
            //{
            //    long departureTimeInLong = DateTimeUtilities.GetTimeStamp(departureTime.Value);
            //    parameters = parameters.Append(new KeyValuePair<string, string>("departure_time", departureTimeInLong.ToString()));
            //}

            DistanceMatrixResponse? response = await HttpClientUtilities.SendRequestAsync
                <DistanceMatrixResponse, object>(baseUrl + "/distancematrix/json", HttpMethod.Get,
                parameters, cancellationToken: cancellationToken);
            if (response != null)
            {
                Row? firstRow = response.Rows.FirstOrDefault();
                if (firstRow == null)
                {
                    throw new Exception("No data for Duration!!");
                }
                Element? firstElement = firstRow.Elements.FirstOrDefault();
                if (firstElement is null || firstElement.Duration is null)
                {
                    throw new Exception("No data for Duration!!");
                }
                return Math.Round(firstElement.Duration.Value.TotalMinutes, 2); // Response Value in meters
            }
            return 0;
        }
    }


}
