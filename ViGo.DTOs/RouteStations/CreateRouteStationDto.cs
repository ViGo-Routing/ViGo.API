using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.DTOs.RouteStations
{
    public class CreateRouteStationDto
    {
        public float Longtitude { get; set; }
        public float Latitude { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        //public int StationIndex { get; set; }
        //public float DistanceFromFirstStation { get; set; }
        //public float DurationFromFirstStation { get; set; }
    }
}
