using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;
using ViGo.Models.Users;

namespace ViGo.Models.Reports
{
    public class ReportViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public ReportType Type { get; set; }
        public string? ReviewerNote { get; set; }
        public Guid? BookingDetailId { get; set; }
        public ReportStatus? Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public BookingDetailViewModel? BookingDetail { get; set; }
        public UserViewModel User { get; set; } = null!;

        public ReportViewModel(Report report, UserViewModel user, BookingDetailViewModel? bookingDetail)
        {
            Id = report.Id;
            UserId = report.UserId;
            Title = report.Title;
            Content = report.Content;
            Type = report.Type;
            ReviewerNote = report.ReviewerNote;
            BookingDetailId = report.BookingDetailId;
            Status = report.Status;
            CreatedTime = report.CreatedTime;
            CreatedBy = report.CreatedBy;
            UpdatedTime = report.UpdatedTime;
            UpdatedBy = report.UpdatedBy;
            IsDeleted = report.IsDeleted;
            User = user;
            BookingDetail = bookingDetail;
        }
    }

    public class ReportSortingParameters : SortingParameters
    {
        public ReportSortingParameters()
        {
            OrderBy = QueryStringUtilities.ToSortingCriteria(
                new SortingCriteria(nameof(Report.CreatedTime)),
                new SortingCriteria(nameof(Report.Status)));
        }
    }

    public class ReportSortByStatusComparer : IComparer<ReportStatus>
    {
        public int Compare(ReportStatus x, ReportStatus y)
        {
            if (x == y) return 0;
            if (x == ReportStatus.PENDING) return 1;
            if (x == ReportStatus.PROCESSED)
            {
                if (y == ReportStatus.PENDING) return -1;
                return 1;
            }
            return -1;
        }
    }
}
