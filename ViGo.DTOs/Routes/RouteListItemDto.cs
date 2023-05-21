using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.RouteRoutines;
using ViGo.DTOs.RouteStations;

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
        public RouteStationListItemDto StartStation { get; set; }
        public RouteStationListItemDto EndStation { get; set; }
        public IList<RouteRoutineListItemDto> RouteRoutines { get; set; }

        public RouteListItemDto(Route route,
            RouteStationListItemDto startStation,
            RouteStationListItemDto endStation,
            IEnumerable<RouteRoutineListItemDto> routines)
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
        }
    }
}
