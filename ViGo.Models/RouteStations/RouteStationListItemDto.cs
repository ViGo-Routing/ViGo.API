using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;

namespace ViGo.Models.RouteStations
{
    public class RouteStationListItemDto
    {
        public int StationIndex { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Longtitude { get; set; }
        public double Latitude { get; set; }
        public double? DistanceFromFirstStation { get; set; }
        public double? DurationFromFirstStation { get; set; }

        public RouteStationListItemDto(RouteStation routeStation, Station station)
        {
            Longtitude = station.Longtitude;
            Latitude = station.Latitude;
            Name = station.Name;
            Address = station.Address;
            StationIndex = routeStation.StationIndex;
            DistanceFromFirstStation = routeStation.DistanceFromFirstStation;
            DurationFromFirstStation = routeStation.DurationFromFirstStation;
        }
    }
}
