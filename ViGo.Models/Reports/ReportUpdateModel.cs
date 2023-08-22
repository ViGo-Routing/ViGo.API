using ViGo.Domain.Enumerations;

namespace ViGo.Models.Reports
{
    public class ReportUpdateModel
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public ReportType? Type { get; set; }

    }
}
