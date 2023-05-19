using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.DTOs.RouteRoutines;
using ViGo.DTOs.RouteStations;

namespace ViGo.DTOs.Routes
{
    public class RouteCreateEditDto
    {
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStationCreateEditDto StartStation { get; set; }
        public RouteStationCreateEditDto EndStation { get; set; }
        public IList<RouteRoutineCreateEditDto> RouteRoutines { get; set; }
    }
}
