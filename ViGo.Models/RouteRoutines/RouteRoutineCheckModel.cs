namespace ViGo.Models.RouteRoutines
{
    public class RouteRoutineCheckModel
    {
        public Guid RouteId { get; set; }
        public IList<RouteRoutineListItemModel> RouteRoutines { get; set; }
            = new List<RouteRoutineListItemModel>();
        //public RouteRoutineAction Action { get; set; } = RouteRoutineAction.CREATE;
    }

    public enum RouteRoutineAction
    {
        CREATE = 1,
        UPDATE = 2
    }

}
