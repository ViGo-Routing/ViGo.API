using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Models.RouteRoutines;
using ViGo.Models.RouteStations;

namespace ViGo.Models.Routes
{
    public class RouteCreateEditDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStationCreateEditDto StartStation { get; set; }
        public RouteStationCreateEditDto EndStation { get; set; }
        public IList<RouteRoutineCreateEditDto> RouteRoutines { get; set; }
    }

    public class RouteChangeStatusDto
    {
        public Guid Id { get; set; }
        public RouteStatus Status { get; set; }
    }
}
