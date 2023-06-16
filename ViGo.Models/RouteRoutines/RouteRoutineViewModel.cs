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
        public DateOnly? RoutineDate { get; set; }
        //public DateOnly StartDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        //public DateOnly EndDate { get; set; }
        public TimeOnly? EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; }

        public RouteRoutineViewModel(RouteRoutine routeRoutine)
        {
            RoutineDate = routeRoutine.RoutineDate.HasValue ?
                DateOnly.FromDateTime(routeRoutine.RoutineDate.Value) : null;
            StartTime = routeRoutine.StartTime.HasValue ?
                TimeOnly.FromTimeSpan(routeRoutine.StartTime.Value) : null;
            EndTime = routeRoutine.EndTime.HasValue ?
                TimeOnly.FromTimeSpan(routeRoutine.EndTime.Value) : null;
            Status = routeRoutine.Status;
        }
    }
}
