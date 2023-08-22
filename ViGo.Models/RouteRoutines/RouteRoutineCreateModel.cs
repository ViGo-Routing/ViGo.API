namespace ViGo.Models.RouteRoutines
{
    public class RouteRoutineCreateModel
    {
        public Guid RouteId { get; set; }
        public IList<RouteRoutineListItemModel> RouteRoutines { get; set; }
            = new List<RouteRoutineListItemModel>();
    }


}
