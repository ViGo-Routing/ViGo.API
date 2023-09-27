using ViGo.Models.GoogleMaps;

namespace ViGo.Models.RouteRoutines
{
    public class RouteRoutineCheckModel
    {
        public Guid RouteId { get; set; }
        public GoogleMapPoint? StartPoint { get; set; }
        public GoogleMapPoint? EndPoint { get; set; }
        public IList<RouteRoutineListItemModel> RouteRoutines { get; set; }
            = new List<RouteRoutineListItemModel>();
        //public RouteRoutineAction Action { get; set; } = RouteRoutineAction.CREATE;
    }

    public class RoundRouteRoutineCheckModel
    {
        public Guid RouteId { get; set; }
        public GoogleMapPoint? StartPoint { get; set; }
        public GoogleMapPoint? EndPoint { get; set; }

        public IList<RouteRoutineListItemModel> RouteRoutines { get; set; }
            = new List<RouteRoutineListItemModel>();

        public IList<RouteRoutineListItemModel> RoundRouteRoutines { get; set; }
            = new List<RouteRoutineListItemModel>();
    }

    public enum RouteRoutineAction
    {
        CREATE = 1,
        UPDATE = 2
    }

}
