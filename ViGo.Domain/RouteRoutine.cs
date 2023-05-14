using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class RouteRoutine
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public Guid UserId { get; set; }
        public DateTime StartDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan EndTime { get; set; }
        public RouteRoutineStatus Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual Route Route { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
