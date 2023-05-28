using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Route
    {
        public Route()
        {
            RouteRoutines = new HashSet<RouteRoutine>();
            RouteStations = new HashSet<RouteStation>();
        }

        public override Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public Guid StartStationId { get; set; }
        public Guid EndStationId { get; set; }
        public double Distance { get; set; }
        public double Duration { get; set; }
        public RouteStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual Station EndStation { get; set; } = null!;
        [JsonIgnore]
        public virtual Station StartStation { get; set; } = null!;
        [JsonIgnore]
        public virtual User User { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<BookingDetail> CustomerBookingDetails { get; set; }
        [JsonIgnore]
        public virtual ICollection<BookingDetail> DriverBookingDetails { get; set; }
        [JsonIgnore]
        public virtual ICollection<RouteRoutine> RouteRoutines { get; set; }
        [JsonIgnore]
        public virtual ICollection<RouteStation> RouteStations { get; set; }
    }
}
