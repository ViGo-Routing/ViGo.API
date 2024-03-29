﻿using ViGo.Domain.Enumerations;

namespace ViGo.Models.Reports
{
    public class ReportCreateModel
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public ReportType Type { get; set; }
        public Guid? BookingDetailId { get; set; }
    }
}
