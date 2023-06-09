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
        public DateOnly StartDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public DateOnly EndDate { get; set; }
        public TimeOnly EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; }

        public RouteRoutineViewModel(RouteRoutine routeRoutine)
        {
            StartDate = DateOnly.FromDateTime(routeRoutine.StartDate);
            StartTime = TimeOnly.FromTimeSpan(routeRoutine.StartTime);
            EndDate = DateOnly.FromDateTime(routeRoutine.EndDate);
            EndTime = TimeOnly.FromTimeSpan(routeRoutine.EndTime);
            Status = routeRoutine.Status;
        }
    }
}
