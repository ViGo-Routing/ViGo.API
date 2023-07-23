using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;

namespace ViGo.Models.RouteRoutines
{
    public class RouteRoutineViewModel
    {
        public Guid Id { get; set; }
        public DateOnly RoutineDate { get; set; }
        //public DateOnly StartDate { get; set; }
        public TimeOnly PickupTime { get; set; }
        //public DateOnly EndDate { get; set; }
        //public TimeOnly? EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; }

        public RouteRoutineViewModel(RouteRoutine routeRoutine)
        {
            Id = routeRoutine.Id;
            RoutineDate = DateOnly.FromDateTime(routeRoutine.RoutineDate);
            PickupTime = TimeOnly.FromTimeSpan(routeRoutine.PickupTime);
            //EndTime = TimeOnly.FromTimeSpan(routeRoutine.EndTime);
            Status = routeRoutine.Status;
        }
    }

    public class RouteRoutineListItemModel : IEquatable<RouteRoutineListItemModel>
    {
        public DateOnly RoutineDate { get; set; }
        //public DateOnly StartDate { get; set; }
        public TimeOnly PickupTime { get; set; }
        //public DateOnly EndDate { get; set; }
        //public TimeOnly EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; } = RouteRoutineStatus.ACTIVE;

        public RouteRoutineListItemModel()
        {

        }

        public RouteRoutineListItemModel(RouteRoutine routeRoutine)
        {
            RoutineDate = DateOnly.FromDateTime(routeRoutine.RoutineDate);
            PickupTime = TimeOnly.FromTimeSpan(routeRoutine.PickupTime);
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
                && this.PickupTime == other.PickupTime
               /* && this.EndTime == other.EndTime*/;
        }
    }

    public class RouteRoutineSortingParameters : SortingParameters
    {
        public RouteRoutineSortingParameters()
        {
            OrderBy = QueryStringUtilities.ToSortingCriteria(
                new SortingCriteria(nameof(RouteRoutine.RoutineDate)),
                new SortingCriteria(nameof(RouteRoutine.PickupTime)));
        }
    }
}
