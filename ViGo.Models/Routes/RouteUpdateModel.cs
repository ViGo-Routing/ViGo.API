using ViGo.Domain.Enumerations;
using ViGo.Models.Bookings;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Stations;

namespace ViGo.Models.Routes
{
    public class RouteUpdateModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStatus Status { get; set; }
        public RoutineType RoutineType { get; set; }
        public RouteType Type { get; set; }
        public StationViewModel StartStation { get; set; }
        public StationViewModel EndStation { get; set; }
    }

    public class RouteChangeStatusModel
    {
        public Guid Id { get; set; }
        public RouteStatus Status { get; set; }
    }

    public class RouteBookingUpdateModel
    {
        public Guid RouteId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStatus Status { get; set; }
        public RoutineType RoutineType { get; set; }
        public RouteType Type { get; set; }
        public StationViewModel StartStation { get; set; }
        public StationViewModel EndStation { get; set; }

        public IList<RouteRoutineListItemModel> RouteRoutines { get; set; }
        public IList<RouteRoutineListItemModel>? RoundTripRoutines { get; set; }
        public BookingUpdateModel BookingUpdate { get; set; }
    }
}
