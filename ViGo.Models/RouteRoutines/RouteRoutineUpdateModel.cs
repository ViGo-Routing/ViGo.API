using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.RouteRoutines
{
    public class RouteRoutineUpdateModel
    {
        public Guid RouteId { get; set; }
        public IList<RouteRoutineListItemModel> RouteRoutines { get; set; }
            = new List<RouteRoutineListItemModel>();
    }

    public class RouteRoutineSingleUpdateModel
    {
        public Guid Id { get; set; }
        //public Guid RouteId { get; set; }
        public DateOnly RoutineDate { get; set; }
        //public DateTime StartDate { get; set; }
        public TimeOnly PickupTime { get; set; }

        //public TimeSpan? StartTime { get; set; }
        //public DateTime EndDate { get; set; }
        //public TimeSpan? EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; } = RouteRoutineStatus.ACTIVE;

    }
}
