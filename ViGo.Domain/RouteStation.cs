using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class RouteStation
    {
        public RouteStation()
        {
            BookingEndRouteStations = new HashSet<Booking>();
            BookingStartRouteStations = new HashSet<Booking>();
        }

        public override Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public Guid StationId { get; set; }
        public int StationIndex { get; set; }
        public double? DistanceFromFirstStation { get; set; }
        public double? DurationFromFirstStation { get; set; }
        public RouteStationStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual Route Route { get; set; } = null!;
        [JsonIgnore]
        public virtual Station Station { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<Booking> BookingEndRouteStations { get; set; }
        [JsonIgnore]
        public virtual ICollection<Booking> BookingStartRouteStations { get; set; }
    }
}
