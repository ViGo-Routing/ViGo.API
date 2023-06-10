using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class RouteRoutine
    {
        public override Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public DateTime RoutineDate { get; set; }
        //public DateTime StartDate { get; set; }
        public TimeSpan StartTime { get; set; }
        //public DateTime EndDate { get; set; }
        public TimeSpan EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual Route Route { get; set; } = null!;
    }
}
