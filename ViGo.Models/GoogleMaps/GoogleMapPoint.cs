using Newtonsoft.Json;

namespace ViGo.Models.GoogleMaps
{
    public class GoogleMapPoint
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        public GoogleMapPoint()
        {

        }

        public GoogleMapPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return Latitude + "%2C" + Longitude;
        }
    }
}
