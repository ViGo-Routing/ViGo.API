using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.RouteRoutines
{
    public class RouteRoutineListItemModel : IEquatable<RouteRoutineListItemModel>
    {
        public DateOnly RoutineDate { get; set; }
        //public DateOnly StartDate { get; set; }
        public TimeOnly StartTime { get; set; }
        //public DateOnly EndDate { get; set; }
        //public TimeOnly EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; } = RouteRoutineStatus.ACTIVE;

        public RouteRoutineListItemModel()
        {

        }

        public RouteRoutineListItemModel(RouteRoutine routeRoutine)
        {
            RoutineDate = DateOnly.FromDateTime(routeRoutine.RoutineDate);
            StartTime = TimeOnly.FromTimeSpan(routeRoutine.StartTime);
            //EndTime = TimeOnly.FromTimeSpan(routeRoutine.EndTime);
            Status = routeRoutine.Status;
        }

        public bool Equals(RouteRoutineListItemModel? other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return this.RoutineDate == other.RoutineDate
                && this.StartTime == other.StartTime
               /* && this.EndTime == other.EndTime*/;
        }
    }

    public class RouteRoutineCreateUpdateModel
    {
        public Guid RouteId { get; set; }
        public IList<RouteRoutineListItemModel> RouteRoutines { get; set; }
            = new List<RouteRoutineListItemModel>();
    }

    public class RouteRoutineSingleUpdateModel
    {
        public Guid Id { get; set; }
        //public Guid RouteId { get; set; }
        public DateTime? RoutineDate { get; set; }
        //public DateTime StartDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        //public DateTime EndDate { get; set; }
        public TimeSpan? EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; }
        
    }
}
