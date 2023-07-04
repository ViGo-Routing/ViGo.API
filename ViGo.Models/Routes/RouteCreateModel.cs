using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Stations;

namespace ViGo.Models.Routes
{
    public class RouteCreateModel
    {
        //public Guid? Id { get; set; }
        public Guid? UserId { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStatus Status { get; set; }
        public RoutineType RoutineType { get; set; }
        public RouteType Type { get; set; } = RouteType.ONE_WAY;
        public StationViewModel StartStation { get; set; }
        public StationViewModel EndStation { get; set; }

        //public IList<RouteRoutineCreateEditModel> RouteRoutines { get; set; }
    }
}
