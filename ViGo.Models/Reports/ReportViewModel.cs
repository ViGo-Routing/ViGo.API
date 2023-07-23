using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Domain;
using ViGo.Models.BookingDetails;
using ViGo.Models.Users;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;

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
        public  BookingDetailViewModel? BookingDetail { get; set; }
        public  UserViewModel User { get; set; } = null!;

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
                new SortingCriteria(nameof(Report.CreatedTime)));
        }
    }
}
