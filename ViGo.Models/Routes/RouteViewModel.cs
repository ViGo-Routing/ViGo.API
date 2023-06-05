using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.RouteRoutines;
using ViGo.Models.RouteStations;
using ViGo.Models.Stations;
using ViGo.Models.Users;

namespace ViGo.Models.Routes
{
    public class RouteViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStatus Status { get; set; }
        public StationViewModel StartStation { get; set; }
        public StationViewModel EndStation { get; set; }
        public IList<RouteRoutineViewModel> RouteRoutines { get; set; }
        public IList<RouteStationViewModel> RouteStations { get; set; }
        public UserViewModel? User { get; set; }

        public RouteViewModel(Route route,
            StationViewModel startStation,
            StationViewModel endStation,
            IEnumerable<RouteRoutineViewModel> routines,
            IEnumerable<RouteStationViewModel> routeStations,
            UserViewModel? user = null)
            : this(route, startStation, endStation, user)
        {
            //Id = route.Id;
            //UserId = route.UserId;
            //Name = route.Name;
            //Distance = route.Distance;
            //Duration = route.Duration;
            //Status = route.Status;
            //StartStation = startStation;
            //EndStation = endStation;
            RouteRoutines = routines.ToList();
            RouteStations = routeStations.ToList();
        }

        public RouteViewModel(Route route,
            StationViewModel startStation,
            StationViewModel endStation,
            UserViewModel? user = null)
        {
            Id = route.Id;
            UserId = route.UserId;
            Name = route.Name;
            Distance = route.Distance;
            Duration = route.Duration;
            Status = route.Status;
            StartStation = startStation;
            EndStation = endStation;
            User = user;
            //RouteRoutines = new List<RouteRoutineListItemDto>();
            //RouteStations = new List<RouteStationListItemDto>();
        }
    }
}
