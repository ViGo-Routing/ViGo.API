using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Report
    {
        public override Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public ReportType Type { get; set; }
        public Guid? BookingDetailId { get; set; }
        public ReportStatus? Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual BookingDetail? BookingDetail { get; set; }
        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
