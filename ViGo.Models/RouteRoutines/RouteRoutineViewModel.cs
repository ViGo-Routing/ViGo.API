using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.RouteRoutines
{
    public class RouteRoutineViewModel
    {
        public DateOnly RoutineDate { get; set; }
        //public DateOnly StartDate { get; set; }
        public TimeOnly PickupTime { get; set; }
        //public DateOnly EndDate { get; set; }
        //public TimeOnly? EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; }

        public RouteRoutineViewModel(RouteRoutine routeRoutine)
        {
            RoutineDate = DateOnly.FromDateTime(routeRoutine.RoutineDate);
            PickupTime = TimeOnly.FromTimeSpan(routeRoutine.PickupTime);
            //EndTime = TimeOnly.FromTimeSpan(routeRoutine.EndTime);
            Status = routeRoutine.Status;
        }
    }
}
