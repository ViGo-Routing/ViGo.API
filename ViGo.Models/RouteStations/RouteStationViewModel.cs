using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.RouteStations
{
    public class RouteStationViewModel
    {
        public int StationIndex { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Guid StationId { get; set; }
        public double Longtitude { get; set; }
        public double Latitude { get; set; }
        public double? DistanceFromFirstStation { get; set; }
        public double? DurationFromFirstStation { get; set; }
        public RouteStationStatus Status { get; set; }

        public RouteStationViewModel(RouteStation routeStation, Station station)
        {
            Longtitude = station.Longtitude;
            Latitude = station.Latitude;
            Name = station.Name;
            Address = station.Address;
            StationId = routeStation.StationId;
            StationIndex = routeStation.StationIndex;
            DistanceFromFirstStation = routeStation.DistanceFromFirstStation;
            DurationFromFirstStation = routeStation.DurationFromFirstStation;
            Status = routeStation.Status;
        }
    }
}
