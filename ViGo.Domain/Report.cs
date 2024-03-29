﻿using Newtonsoft.Json;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Report
    {
        public override Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ReviewerNote { get; set; }
        public Guid? BookingDetailId { get; set; }
        public ReportType Type { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual BookingDetail? BookingDetail { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; } = null!;
    }
}
