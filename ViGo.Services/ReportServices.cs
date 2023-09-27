using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.Notifications;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Reports;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;

namespace ViGo.Services
{
    public class ReportServices : UseNotificationServices
    {
        public ReportServices(IUnitOfWork work, ILogger logger) : base(work, logger) { }

        public async Task<IPagedEnumerable<ReportViewModel>> GetAllReports(
            PaginationParameter pagination, ReportSortingParameters sorting,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Report> reports = await work.Reports.GetAllAsync(cancellationToken: cancellationToken);

            reports = reports.Sort(sorting.OrderBy);

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
            PaginationParameter pagination, ReportSortingParameters sorting,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            Guid userId = IdentityUtilities.GetCurrentUserId();
            IEnumerable<Report> reports = await work.Reports.GetAllAsync(
                q => q.Where(x => x.UserId.Equals(userId)), cancellationToken: cancellationToken);

            //reports = reports.Sort(sorting.OrderBy);

            reports = reports.OrderByDescending(
                r => r.Status, new ReportSortByStatusComparer())
                .ThenByDescending(r => r.CreatedTime);

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
            if (reportCreate.Type == ReportType.BOOKER_NOT_COMING ||
                reportCreate.Type == ReportType.DRIVER_NOT_COMING ||
                reportCreate.Type == ReportType.DRIVER_CANCEL_TRIP)
            {
                if (!reportCreate.BookingDetailId.HasValue)
                {
                    throw new ApplicationException("Thiếu dữ liệu chuyến đi!!");
                }
            }

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

        public async Task<(ReportViewModel, Guid?)> AdminUpdateReport(Guid id, ReportAdminUpdateModel reportAdminUpdate,
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                throw new ApplicationException("Bạn không có vai trò phù hợp để thực hiện chức năng này!");
            }
            var currentReport = await work.Reports.GetAsync(id, cancellationToken: cancellationToken);
            if (currentReport is null)
            {
                throw new ApplicationException("ID không tồn tại!");
            }

            if (reportAdminUpdate.Status != ReportStatus.PENDING)
            {
                throw new ApplicationException("Trạng thái không phù hợp!!");
            }
            if (reportAdminUpdate.ReviewerNote != null) currentReport.ReviewerNote = reportAdminUpdate.ReviewerNote;
            if (reportAdminUpdate.Status != null) currentReport.Status = reportAdminUpdate.Status.Value;
            //if (reportAdminUpdate.IsDeleted != null) currentReport.IsDeleted = (bool)reportAdminUpdate.IsDeleted;

            await work.Reports.UpdateAsync(currentReport);

            BookingDetail? bookingDetail = null;
            string title = "";
            string description = "";
            Guid? customerId = null;

            // Handle for report type
            if (currentReport.Status == ReportStatus.PROCESSED)
            {
                if (currentReport.Type == ReportType.DRIVER_NOT_COMING)
                {

                }
                else if (currentReport.Type == ReportType.BOOKER_NOT_COMING)
                {
                    // Change status to complete and driver gets paid
                    bookingDetail = await work.BookingDetails.GetAsync(
                        currentReport.BookingDetailId.Value, cancellationToken: cancellationToken);

                    bookingDetail.Status = BookingDetailStatus.ARRIVE_AT_DROPOFF;
                    bookingDetail.DropoffTime = DateTimeUtilities.GetDateTimeVnNow();

                    await work.BookingDetails.UpdateAsync(bookingDetail);

                    Station startStationDropOff = await work.Stations.GetAsync(
                           bookingDetail.StartStationId, cancellationToken: cancellationToken);

                    Station endStation = await work.Stations.GetAsync(
                        bookingDetail.EndStationId, cancellationToken: cancellationToken);

                    title = "Chuyến đi của bạn đã hoàn thành!";
                    description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStationDropOff.Name} đến {endStation.Name}";
                }
            }

            await work.SaveChangesAsync(cancellationToken);

            if (bookingDetail != null && !string.IsNullOrEmpty(title)
                && !string.IsNullOrEmpty(description))
            {
                Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);

                customerId = booking.CustomerId;

                // Send notification to Customer and Driver
                User customer = await work.Users.GetAsync(booking.CustomerId,
                    cancellationToken: cancellationToken);

                string? customerFcm = customer.FcmToken;

                Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                {
                    {"action", NotificationAction.BookingDetail },
                    { "bookingDetailId", bookingDetail.Id.ToString() },
                };

                if (bookingDetail.DriverId.HasValue)
                {
                    User driver = await work.Users.GetAsync(
                    bookingDetail.DriverId.Value, cancellationToken: cancellationToken);
                    string? driverFcm = driver.FcmToken;

                    // Send to driver
                    if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
                    {
                        NotificationCreateModel driverNotification = new NotificationCreateModel()
                        {
                            UserId = bookingDetail.DriverId.Value,
                            Title = title,
                            Description = description,
                            Type = NotificationType.SPECIFIC_USER
                        };

                        await notificationServices.CreateFirebaseNotificationAsync(
                            driverNotification, driverFcm, dataToSend, cancellationToken);
                    }
                }


                // Send to customer
                if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
                {

                    NotificationCreateModel customerNotification = new NotificationCreateModel()
                    {
                        UserId = customer.Id,
                        Title = title,
                        Description = description,
                        Type = NotificationType.SPECIFIC_USER
                    };

                    await notificationServices.CreateFirebaseNotificationAsync(
                        customerNotification, customerFcm, dataToSend, cancellationToken);

                }

            }

            User user = await work.Users.GetAsync(currentReport.UserId, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);

            BookingDetailViewModel? bookingDetailView = null;
            if (currentReport.BookingDetailId.HasValue)
            {
                bookingDetail = await work.BookingDetails.GetAsync(currentReport.BookingDetailId.Value,
                cancellationToken: cancellationToken);

                bookingDetailView = new BookingDetailViewModel(bookingDetail);

            }

            // Send notification for report being processed
            string? userFcm = user.FcmToken;

            if (!string.IsNullOrEmpty(userFcm))
            {
                NotificationCreateModel reportNotification = new NotificationCreateModel()
                {
                    UserId = currentReport.UserId,
                    Title = "Báo cáo của bạn đã " + (currentReport.Status == ReportStatus.PROCESSED ? "được xử lý" : "bị từ chối"),
                    Description = string.IsNullOrEmpty(currentReport.ReviewerNote) ? "" : currentReport.ReviewerNote,
                    Type = NotificationType.SPECIFIC_USER
                };
                Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                {
                    {"action", NotificationAction.Report },
                    { "reportId", currentReport.Id.ToString() },
                };

                await notificationServices.CreateFirebaseNotificationAsync(
                    reportNotification, userFcm, dataToSend, cancellationToken);
            }

            ReportViewModel reportView = new ReportViewModel(currentReport, userViewModel, bookingDetailView);
            return (reportView, customerId);

        }
    }
}
