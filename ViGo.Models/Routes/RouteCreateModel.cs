﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Models.RouteRoutines;
using ViGo.Models.RouteStations;

namespace ViGo.Models.Routes
{
    public class RouteCreateModel
    {
        //public Guid? Id { get; set; }
        public Guid? UserId { get; set; }
        public string Name { get; set; }
        public double? Distance { get; set; }
        public double? Duration { get; set; }
        public RouteStatus Status { get; set; }
        public RoutineType RoutineType { get; set; }
        public RouteType RouteType { get; set; }
        public RouteStationCreateEditModel? StartStation { get; set; }
        public RouteStationCreateEditModel? EndStation { get; set; }

        //public IList<RouteRoutineCreateEditModel> RouteRoutines { get; set; }
    }
}
