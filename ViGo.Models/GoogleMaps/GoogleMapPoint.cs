﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.GoogleMaps
{
    public class GoogleMapPoint
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        public override string ToString()
        {
            return Latitude + "%2C" + Longitude;
        }
    }
}
