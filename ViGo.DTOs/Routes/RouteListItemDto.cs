using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.RouteRoutines;
using ViGo.DTOs.RouteStations;
using ViGo.DTOs.Stations;

namespace ViGo.DTOs.Routes
{
    public class RouteListItemDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStatus Status { get; set; }
        public StationListItemDto StartStation { get; set; }
        public StationListItemDto EndStation { get; set; }
        public IList<RouteRoutineListItemDto> RouteRoutines { get; set; }
        public IList<RouteStationListItemDto> RouteStations { get; set; }

        public RouteListItemDto(Route route,
            StationListItemDto startStation,
            StationListItemDto endStation,
            IEnumerable<RouteRoutineListItemDto> routines,
            IEnumerable<RouteStationListItemDto> routeStations)
        {
            Id = route.Id;
            UserId = route.UserId;
            Name = route.Name;
            Distance = route.Distance;
            Duration = route.Duration;
            Status = route.Status;
            StartStation = startStation;
            EndStation = endStation;
            RouteRoutines = routines.ToList();
            RouteStations = routeStations.ToList();
        }

        public RouteListItemDto(Route route,
            StationListItemDto startStation,
            StationListItemDto endStation)
        {
            Id = route.Id;
            UserId = route.UserId;
            Name = route.Name;
            Distance = route.Distance;
            Duration = route.Duration;
            Status = route.Status;
            StartStation = startStation;
            EndStation = endStation;
            RouteRoutines = new List<RouteRoutineListItemDto>();
            RouteStations = new List<RouteStationListItemDto>();
        }
    }
}
