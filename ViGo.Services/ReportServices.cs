using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.Reports;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Models.QueryString.Pagination;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Models.QueryString;

namespace ViGo.Services
{
    public class ReportServices : BaseServices
    {
        public ReportServices(IUnitOfWork work, ILogger logger) : base(work, logger) { }

        public async Task<IPagedEnumerable<ReportViewModel>> GetAllReports(
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Report> reports = await work.Reports.GetAllAsync(cancellationToken: cancellationToken);
            int totalRecords = reports.Count();
            reports = reports.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> userIds = reports.Select(id => id.UserId);
            IEnumerable<User> users = await work.Users.GetAllAsync(
                q => q.Where(x => userIds.Contains(x.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserViewModel> userViews = from user in users
                                                   select new UserViewModel(user);

            IEnumerable<Guid> bookingDetailIDs = reports.Select(id => id.BookingDetailId.Value);
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails.GetAllAsync(
                q => q.Where(x => bookingDetailIDs.Contains(x.Id)), cancellationToken: cancellationToken);
            IEnumerable<BookingDetailViewModel> bookingDetailViews = from bookingDetail in bookingDetails
                                                                     select new BookingDetailViewModel(bookingDetail);


            IEnumerable<ReportViewModel> reportViews = from report in reports
                                                       join userView in userViews
                                                           on report.UserId equals userView.Id
                                                       join bookingDetailView in bookingDetailViews
                                                           on report.BookingDetailId equals bookingDetailView.Id
                                                       select new ReportViewModel(report, userView, bookingDetailView);
            return reportViews.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<IPagedEnumerable<ReportViewModel>> GetAllReportsByUserID(
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            Guid userId = IdentityUtilities.GetCurrentUserId();
            IEnumerable<Report> reports = await work.Reports.GetAllAsync(
                q => q.Where(x => x.UserId.Equals(userId)),cancellationToken: cancellationToken);
            int totalRecords = reports.Count();

            reports = reports.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;
            IEnumerable<Guid> userIds = reports.Select(id => id.UserId);
            IEnumerable<User> users = await work.Users.GetAllAsync(
                q => q.Where(x => userIds.Contains(x.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserViewModel> userViews = from user in users
                                                   select new UserViewModel(user);

            IEnumerable<Guid> bookingDetailIDs = reports.Select(id => id.BookingDetailId.Value);
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails.GetAllAsync(
                q => q.Where(x => bookingDetailIDs.Contains(x.Id)), cancellationToken: cancellationToken);
            IEnumerable<BookingDetailViewModel> bookingDetailViews = from bookingDetail in bookingDetails
                                                                     select new BookingDetailViewModel(bookingDetail);


            IEnumerable<ReportViewModel> reportViews = from report in reports
                                                       join userView in userViews
                                                           on report.UserId equals userView.Id
                                                       join bookingDetailView in bookingDetailViews
                                                           on report.BookingDetailId equals bookingDetailView.Id
                                                       select new ReportViewModel(report, userView, bookingDetailView);
            return reportViews.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);

        }

        public async Task<ReportViewModel> GetReportById(Guid id, CancellationToken cancellationToken)
        {
            Report report = await work.Reports.GetAsync(id, cancellationToken: cancellationToken);
            if (report is null)
            {
                throw new ApplicationException("ID không tồn tại!");
            }

            Guid userID = report.UserId;
            User user = await work.Users.GetAsync(userID, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);

            Guid bookingDetailID = report.BookingDetailId.Value;
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(bookingDetailID, cancellationToken: cancellationToken);
            BookingDetailViewModel bookingDetailView = new BookingDetailViewModel(bookingDetail);

            ReportViewModel reportView = new ReportViewModel(report, userViewModel, bookingDetailView);

            return reportView;
        }

        public async Task<ReportViewModel> CreateReport(ReportCreateModel reportCreate, CancellationToken cancellationToken)
        {
            Report newReport = new Report
            {
                UserId = IdentityUtilities.GetCurrentUserId(),
                Title = reportCreate.Title,
                Content = reportCreate.Content,
                Type = reportCreate.Type,
                Status = ReportStatus.PENDING,
                BookingDetailId = reportCreate.BookingDetailId,
                IsDeleted = false,
            };

            await work.Reports.InsertAsync(newReport, cancellationToken: cancellationToken);
            var result = await work.SaveChangesAsync(cancellationToken: cancellationToken);

            User user = await work.Users.GetAsync(newReport.UserId, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(newReport.BookingDetailId.Value, cancellationToken: cancellationToken);
            BookingDetailViewModel bookingDetailView = new BookingDetailViewModel(bookingDetail);
            ReportViewModel reportView = new ReportViewModel(newReport, userViewModel, bookingDetailView);
            if (result > 0)
            {
                return reportView;
            }
            return null;


        }

        public async Task<ReportViewModel> UserUpdateReport(Guid id, ReportUpdateModel reportUpdate)
        {
            var currentReport = await work.Reports.GetAsync(id);
            if (currentReport is null)
            {
                throw new ApplicationException("ID không tồn tại!");
            }

            if (!IdentityUtilities.IsAdmin()
                && !IdentityUtilities.IsStaff())
            {
                if (currentReport.UserId != IdentityUtilities.GetCurrentUserId())
                {
                    throw new ApplicationException("Bạn không có vai trò phù hợp để thực hiện chức năng này!");
                }
            }

            if (currentReport.Status != ReportStatus.PENDING)
            {
                throw new ApplicationException("Đơn báo cáo của bạn đã được Admin xử lý nên không thể chỉnh sửa!");
            }

            if (currentReport != null)
            {
                if (reportUpdate.Title != null) currentReport.Title = reportUpdate.Title;
                if (reportUpdate.Content != null) currentReport.Content = reportUpdate.Content;
                if (reportUpdate.Type != null) currentReport.Type = (ReportType)reportUpdate.Type;
            }

            await work.Reports.UpdateAsync(currentReport!);
            await work.SaveChangesAsync();

            User user = await work.Users.GetAsync(currentReport.UserId);
            UserViewModel userViewModel = new UserViewModel(user);
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(currentReport.BookingDetailId.Value);
            BookingDetailViewModel bookingDetailView = new BookingDetailViewModel(bookingDetail);
            ReportViewModel reportView = new ReportViewModel(currentReport, userViewModel, bookingDetailView);

            return reportView;
        }

        public async Task<ReportViewModel> AdminUpdateReport(Guid id, ReportAdminUpdateModel reportAdminUpdate)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                throw new ApplicationException("Bạn không có vai trò phù hợp để thực hiện chức năng này!");
            }
            var currentReport = await work.Reports.GetAsync(id);
            if (currentReport is null)
            {
                throw new ApplicationException("ID không tồn tại!");
            }
            if (currentReport != null)
            {
                if (reportAdminUpdate.ReviewerNote != null) currentReport.ReviewerNote = reportAdminUpdate.ReviewerNote;
                if (reportAdminUpdate.Status != null) currentReport.Status = reportAdminUpdate.Status;
                if (reportAdminUpdate.IsDeleted != null) currentReport.IsDeleted = (bool)reportAdminUpdate.IsDeleted;
            }

            await work.Reports.UpdateAsync(currentReport!);
            await work.SaveChangesAsync();

            User user = await work.Users.GetAsync(currentReport.UserId);
            UserViewModel userViewModel = new UserViewModel(user);
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(currentReport.BookingDetailId.Value);
            BookingDetailViewModel bookingDetailView = new BookingDetailViewModel(bookingDetail);
            ReportViewModel reportView = new ReportViewModel(currentReport, userViewModel, bookingDetailView);
            return reportView;

        }
    }
}
