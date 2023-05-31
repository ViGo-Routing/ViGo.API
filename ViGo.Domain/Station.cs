using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Station
    {
        public Station()
        {
            RouteEndStations = new HashSet<Route>();
            RouteStartStations = new HashSet<Route>();
            RouteStations = new HashSet<RouteStation>();
        }

        public override Guid Id { get; set; }
        public double Longtitude { get; set; }
        public double Latitude { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public StationStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual ICollection<Route> RouteEndStations { get; set; }
        [JsonIgnore]
        public virtual ICollection<Route> RouteStartStations { get; set; }
        [JsonIgnore]
        public virtual ICollection<RouteStation> RouteStations { get; set; }
    }
}
