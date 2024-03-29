﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.GoogleMaps;
using ViGo.Models.Notifications;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Google;
using ViGo.Utilities.Google.Firebase;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class BookingDetailServices : UseNotificationServices
    {
        public BookingDetailServices(IUnitOfWork work,
            ILogger logger) : base(work, logger)
        {
        }

        public async Task<BookingDetailViewModel?> GetBookingDetailAsync(
            Guid bookingDetailId, CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(bookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail == null)
            {
                return null;
            }

            UserViewModel? driverDto = null;
            if (bookingDetail.DriverId.HasValue)
            {
                User driver = await work.Users.GetAsync(bookingDetail.DriverId.Value, cancellationToken: cancellationToken);
                driverDto = new UserViewModel(driver);
            }

            //Route customerRoute = await work.Routes.GetAsync(bookingDetail.CustomerRouteId, cancellationToken: cancellationToken);

            //IEnumerable<Guid> stationIds = (new List<Guid>
            //{
            //    customerRoute.StartStationId,
            //    customerRoute.EndStationId,

            //}).Distinct();

            //IEnumerable<Guid> stationIds = new List<Guid>();

            //Route? driverRoute = null;
            //if (bookingDetail.DriverRouteId.HasValue)
            //{
            //    driverRoute = await work.Routes
            //        .GetAsync(bookingDetail.DriverRouteId.Value, cancellationToken: cancellationToken);
            //    if (driverRoute.StartStationId.HasValue && driverRoute.EndStationId.HasValue)
            //    {
            //        stationIds = stationIds.Append(driverRoute.StartStationId.Value)
            //            .Append(driverRoute.EndStationId.Value);
            //    }

            //}

            //IEnumerable<Station> stations = await work.Stations
            //    .GetAllAsync(query => query.Where(
            //        s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

            //RouteViewModel customerRouteDto = new RouteViewModel(
            //    customerRoute,
            //    new StationViewModel(
            //        stations.SingleOrDefault(s => s.Id.Equals(customerRoute.StartStationId)), 1),
            //    new StationViewModel(
            //        stations.SingleOrDefault(s => s.Id.Equals(customerRoute.EndStationId)), 2));

            //RouteViewModel? driverRouteDto = null;
            //if (driverRoute != null)
            //{
            //    driverRouteDto = new RouteViewModel(
            //    driverRoute,
            //    driverRoute.StartStationId.HasValue ?
            //    new StationViewModel(
            //        stations.SingleOrDefault(s => s.Id.Equals(driverRoute.StartStationId.Value)), 1) : null,
            //    driverRoute.EndStationId.HasValue ?
            //    new StationViewModel(
            //        stations.SingleOrDefault(s => s.Id.Equals(driverRoute.EndStationId)), 2) : null);
            //}

            RouteRoutine customerRoutine = await work.RouteRoutines
                .GetAsync(bookingDetail.CustomerRouteRoutineId, cancellationToken: cancellationToken);
            RouteRoutineViewModel customerRoutineModel = new RouteRoutineViewModel(customerRoutine);
            //BookingDetailViewModel dto = new BookingDetailViewModel(
            //    bookingDetail, driverDto /*customerRouteDto,*/ /*driverRouteDto*/);
            ////BookingDetailViewModel dto = new BookingDetailViewModel(bookingDetail);
            Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId, includeDeleted: true,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId, includeDeleted: true,
                cancellationToken: cancellationToken);

            BookingDetailViewModel bookingDetailViewModel = new BookingDetailViewModel(bookingDetail, customerRoutineModel,
                 new StationViewModel(startStation), new StationViewModel(endStation), driverDto);

            return bookingDetailViewModel;

        }

        public async Task<IPagedEnumerable<BookingDetailViewModel>>
            GetUserBookingDetailsAsync(Guid userId, Guid? bookingId,
            PaginationParameter pagination, BookingDetailSortingParameters sorting,
            BookingDetailFilterParameters filters,
            HttpContext context, CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                userId = IdentityUtilities.GetCurrentUserId();
            }

            //if (role != UserRole.CUSTOMER && role != UserRole.DRIVER)
            //{
            //    throw new ApplicationException("Vai trò người dùng không hợp lệ!!");
            //}

            User user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            if (user is null)
            {
                throw new ApplicationException("Người dùng không tồn tại!!!");
            }

            IEnumerable<BookingDetail> bookingDetails = new List<BookingDetail>();
            if (user.Role == UserRole.CUSTOMER)
            {
                if (bookingId.HasValue)
                {
                    bookingDetails = await work.BookingDetails.GetAllAsync(
                        query => query.Where(d => d.BookingId.Equals(bookingId.Value)),
                        cancellationToken: cancellationToken);

                }
                else
                {
                    IEnumerable<Booking> bookings = await work.Bookings
                    .GetAllAsync(query => query.Where(b => b.CustomerId.Equals(userId)),
                    cancellationToken: cancellationToken);
                    IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

                    bookingDetails = await work.BookingDetails.GetAllAsync(
                        query => query.Where(d => bookingIds.Contains(d.BookingId)),
                        cancellationToken: cancellationToken);
                }


            }
            else if (user.Role == UserRole.DRIVER)
            {
                bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                    bd.DriverId.Value.Equals(userId)
                    && (bookingId.HasValue ? bd.BookingId.Equals(bookingId.Value) : true)
                    /*&& bd.Status != BookingDetailStatus.CANCELLED*/), cancellationToken: cancellationToken);
            }

            bookingDetails = await FilterBookingDetailsAsync(bookingDetails, filters, cancellationToken);

            bookingDetails = bookingDetails.Sort(sorting.OrderBy);

            int totalRecords = bookingDetails.Count();

            bookingDetails = bookingDetails.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data;

            if (!bookingDetails.Any())
            {
                return new List<BookingDetailViewModel>().ToPagedEnumerable(pagination.PageNumber,
                    pagination.PageSize, 0, context);
            }

            //IEnumerable<Guid> routeIds = (bookingDetails.Where(bd => bd.DriverRouteId.HasValue)
            //    .Select(bd =>
            //    bd.DriverRouteId.Value))
            //    .Distinct();

            //IEnumerable<Route> routes = await work.Routes
            //    .GetAllAsync(query => query.Where(
            //        r => routeIds.Contains(r.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> stationIds = bookingDetails.Select(
               b => b.StartStationId).Concat(bookingDetails.Select(
               b => b.EndStationId)).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), includeDeleted: true,
                    cancellationToken: cancellationToken);

            IEnumerable<Guid> customerRoutineIds = bookingDetails.Select(bd => bd.CustomerRouteRoutineId)
                .Distinct();

            IEnumerable<RouteRoutine> customerRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => customerRoutineIds.Contains(r.Id)), cancellationToken: cancellationToken);

            IEnumerable<BookingDetailViewModel> dtos =
                from bookingDetail in bookingDetails
                    //join customerRoute in routes
                    //    on bookingDetail.CustomerRouteId equals customerRoute.Id
                join customerStartStation in stations
                    on bookingDetail.StartStationId equals customerStartStation.Id
                join customerEndStation in stations
                    on bookingDetail.EndStationId equals customerEndStation.Id
                //join driverRoute in routes
                //    on bookingDetail.DriverRouteId equals driverRoute.Id
                //join driverStartStation in stations
                //    on driverRoute.StartStationId equals driverStartStation.Id
                //join driverEndStation in stations
                //    on driverRoute.EndStationId equals driverEndStation.Id
                join customerRoutine in customerRoutines
                    on bookingDetail.CustomerRouteRoutineId equals customerRoutine.Id
                select new BookingDetailViewModel(
                    bookingDetail, new RouteRoutineViewModel(customerRoutine),
                    new StationViewModel(customerStartStation),
                    new StationViewModel(customerEndStation), null);
            //new RouteViewModel(customerRoute,
            //    new StationViewModel(customerStartStation, 1),
            //    new StationViewModel(customerEndStation, 2)),
            /*new RouteViewModel(driverRoute,
                new StationViewModel(driverStartStation),
                //new StationViewModel(driverEndStation))); */
            //IEnumerable<BookingDetailViewModel> dtos =
            //    from bookingDetail in bookingDetails
            //    select new BookingDetailViewModel(bookingDetail);

            return dtos.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<IPagedEnumerable<BookingDetailViewModel>>
            GetBookingDetailsAsync(Guid bookingId,
            PaginationParameter pagination, BookingDetailSortingParameters sorting,
            BookingDetailFilterParameters filters,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    d => d.BookingId.Equals(bookingId)), cancellationToken: cancellationToken);

            bookingDetails = await FilterBookingDetailsAsync(bookingDetails, filters, cancellationToken);

            bookingDetails = bookingDetails.Sort(sorting.OrderBy);

            int totalRecords = bookingDetails.Count();

            bookingDetails = bookingDetails
                .ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> driverIds = bookingDetails.Where(
                d => d.DriverId.HasValue).Select(d => d.DriverId.Value);
            IEnumerable<User> drivers = await work.Users
                .GetAllAsync(query => query.Where(
                    u => driverIds.Contains(u.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> customerRoutineIds = bookingDetails.Select(bd => bd.CustomerRouteRoutineId)
                .Distinct();

            IEnumerable<RouteRoutine> customerRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => customerRoutineIds.Contains(r.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> stationIds = bookingDetails.Select(
               b => b.StartStationId).Concat(bookingDetails.Select(
               b => b.EndStationId)).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), includeDeleted: true,
                    cancellationToken: cancellationToken);

            IEnumerable<BookingDetailViewModel> dtos =
                new List<BookingDetailViewModel>();
            foreach (BookingDetail bookingDetail in bookingDetails)
            {
                RouteRoutine customerRoutine = customerRoutines.SingleOrDefault(
                    r => r.Id.Equals(bookingDetail.CustomerRouteRoutineId));

                Station startStation = stations.SingleOrDefault(s => s.Id.Equals(bookingDetail.StartStationId));
                Station endStation = stations.SingleOrDefault(s => s.Id.Equals(bookingDetail.EndStationId));

                BookingDetailViewModel model = new BookingDetailViewModel(bookingDetail, new RouteRoutineViewModel(customerRoutine),
                        new StationViewModel(startStation), new StationViewModel(endStation), null);

                if (bookingDetail.DriverId.HasValue)
                {
                    User driver = drivers.SingleOrDefault(u => u.Id.Equals(bookingDetail.DriverId));
                    model.Driver = new UserViewModel(driver);
                }
                //else
                //{
                //    dtos = dtos.Append(new BookingDetailViewModel(bookingDetail, new RouteRoutineViewModel(customerRoutine),
                //        null));
                //}
                dtos = dtos.Append(model);
            }

            return dtos.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<BookingDetailViewModel?> GetUpcomingTripAsync(Guid userId,
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                userId = IdentityUtilities.GetCurrentUserId();
            }

            User user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            if (user is null)
            {
                throw new ApplicationException("Người dùng không tồn tại!!!");
            }
            if (user.Role != UserRole.CUSTOMER && user.Role != UserRole.DRIVER)
            {
                throw new ApplicationException("Vai trò người dùng không hợp lệ!!");
            }

            DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();
            IEnumerable<BookingDetail> bookingDetails = new List<BookingDetail>();
            if (user.Role == UserRole.CUSTOMER)
            {
                IEnumerable<Booking> bookings = await work.Bookings
                    .GetAllAsync(query => query.Where(b => b.CustomerId.Equals(userId)
                    && b.Status == BookingStatus.CONFIRMED),
                cancellationToken: cancellationToken);
                IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

                bookingDetails = await work.BookingDetails.GetAllAsync(
                    query => query.Where(d => bookingIds.Contains(d.BookingId)
                    && (d.Status == BookingDetailStatus.PENDING_ASSIGN ||
                    d.Status == BookingDetailStatus.ASSIGNED)),
                    cancellationToken: cancellationToken);

            }
            else if (user.Role == UserRole.DRIVER)
            {
                bookingDetails = await work.BookingDetails
                    .GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                    bd.DriverId.Value.Equals(userId)
                    && bd.Status == BookingDetailStatus.ASSIGNED
                    /*&& bd.Status != BookingDetailStatus.CANCELLED*/), cancellationToken: cancellationToken);
            }
            bookingDetails = bookingDetails.Where(
                d => d.PickUpDateTime() > vnNow).OrderBy(d => d.Date)
                .ThenBy(d => d.CustomerDesiredPickupTime);
            if (!bookingDetails.Any())
            {
                return null;
            }

            BookingDetail upcoming = bookingDetails.First();

            UserViewModel? driverDto = null;
            if (upcoming.DriverId.HasValue)
            {
                User driver = await work.Users.GetAsync(upcoming.DriverId.Value, cancellationToken: cancellationToken);
                driverDto = new UserViewModel(driver);
            }

            RouteRoutine customerRoutine = await work.RouteRoutines
                .GetAsync(upcoming.CustomerRouteRoutineId, cancellationToken: cancellationToken);
            RouteRoutineViewModel customerRoutineModel = new RouteRoutineViewModel(customerRoutine);

            Station startStation = await work.Stations.GetAsync(upcoming.StartStationId, includeDeleted: true,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(upcoming.EndStationId, includeDeleted: true,
                cancellationToken: cancellationToken);

            BookingDetailViewModel bookingDetailViewModel = new BookingDetailViewModel(upcoming, customerRoutineModel,
                 new StationViewModel(startStation), new StationViewModel(endStation), driverDto);

            return bookingDetailViewModel;
        }

        public async Task<BookingDetailViewModel?> GetCurrentTripAsync(Guid userId,
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                userId = IdentityUtilities.GetCurrentUserId();
            }

            User user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
            if (user is null)
            {
                throw new ApplicationException("Người dùng không tồn tại!!!");
            }
            if (user.Role != UserRole.CUSTOMER && user.Role != UserRole.DRIVER)
            {
                throw new ApplicationException("Vai trò người dùng không hợp lệ!!");
            }

            IEnumerable<BookingDetail> bookingDetails = new List<BookingDetail>();
            if (user.Role == UserRole.CUSTOMER)
            {
                IEnumerable<Booking> bookings = await work.Bookings
                    .GetAllAsync(query => query.Where(b => b.CustomerId.Equals(userId)
                    && b.Status == BookingStatus.CONFIRMED),
                cancellationToken: cancellationToken);
                IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

                bookingDetails = await work.BookingDetails.GetAllAsync(
                    query => query.Where(d => bookingIds.Contains(d.BookingId)
                    && (d.Status == BookingDetailStatus.GOING_TO_PICKUP ||
                    d.Status == BookingDetailStatus.ARRIVE_AT_PICKUP ||
                    d.Status == BookingDetailStatus.GOING_TO_DROPOFF ||
                    d.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)),
                    cancellationToken: cancellationToken);

            }
            else if (user.Role == UserRole.DRIVER)
            {
                bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                    bd.DriverId.Value.Equals(userId)
                    /*&& bd.Status != BookingDetailStatus.CANCELLED*/
                    && (bd.Status == BookingDetailStatus.GOING_TO_PICKUP ||
                    bd.Status == BookingDetailStatus.ARRIVE_AT_PICKUP ||
                    bd.Status == BookingDetailStatus.GOING_TO_DROPOFF ||
                    bd.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)), cancellationToken: cancellationToken);
            }
            DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();

            bookingDetails = bookingDetails
                .OrderByDescending(d => d.Date)
                .ThenByDescending(d => d.CustomerDesiredPickupTime);
            if (!bookingDetails.Any())
            {
                return null;
            }

            IEnumerable<Report> reports = await work.Reports.GetAllAsync(
                query => query.Where(
                    r => r.Status == ReportStatus.PENDING
                    && r.UserId.Equals(userId)
                    && (r.Type == ReportType.DRIVER_NOT_COMING
                        || r.Type == ReportType.BOOKER_NOT_COMING)), cancellationToken: cancellationToken);

            if (reports.Any())
            {
                IEnumerable<Guid> reportedTrips = reports.Where(r => r.BookingDetailId.HasValue)
                    .Select(r => r.BookingDetailId.Value);

                bookingDetails = bookingDetails.Where(
                    d => !reportedTrips.Contains(d.Id));

            }

            if (!bookingDetails.Any())
            {
                return null;
            }

            BookingDetail current = bookingDetails.First();

            UserViewModel? driverDto = null;
            if (current.DriverId.HasValue)
            {
                User driver = await work.Users.GetAsync(current.DriverId.Value, cancellationToken: cancellationToken);
                driverDto = new UserViewModel(driver);
            }

            RouteRoutine customerRoutine = await work.RouteRoutines
                .GetAsync(current.CustomerRouteRoutineId, cancellationToken: cancellationToken);
            RouteRoutineViewModel customerRoutineModel = new RouteRoutineViewModel(customerRoutine);

            Station startStation = await work.Stations.GetAsync(current.StartStationId, includeDeleted: true,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(current.EndStationId, includeDeleted: true,
                cancellationToken: cancellationToken);

            BookingDetailViewModel bookingDetailViewModel = new BookingDetailViewModel(current, customerRoutineModel,
                 new StationViewModel(startStation), new StationViewModel(endStation), driverDto);

            return bookingDetailViewModel;
        }

        public async Task<(BookingDetail, Guid)> UpdateBookingDetailStatusAsync(
            BookingDetailUpdateStatusModel updateDto, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(updateDto.Status))
            {
                throw new ApplicationException("Trạng thái Booking Detail không hợp lệ!");
            }

            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(updateDto.BookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail == null)
            {
                throw new ApplicationException("Booking Detail không tồn tại!!");
            }

            if (bookingDetail.Status == updateDto.Status)
            {
                throw new ApplicationException("Trạng thái Booking Detail không hợp lệ!!");
            }

            if (updateDto.Status == BookingDetailStatus.ARRIVE_AT_PICKUP
                || updateDto.Status == BookingDetailStatus.GOING_TO_PICKUP
                || updateDto.Status == BookingDetailStatus.GOING_TO_DROPOFF
                || updateDto.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
            {
                if (!updateDto.Time.HasValue)
                {
                    throw new ApplicationException("Dữ liệu truyền đi bị thiếu thời gian cập nhật!!");
                }

                // TODO Code
                // Time validation
                if (updateDto.Status == BookingDetailStatus.GOING_TO_PICKUP)
                {
                    DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();
                    if ((bookingDetail.PickUpDateTime() - vnNow)
                        .TotalMinutes <= -2)
                    {
                        throw new ApplicationException("Chuyến đi trong quá khứ, không thể cập nhật trạng thái!");
                    }

                    Setting pickupTimeSetting = await work.Settings.GetAsync(
                        s => s.Key.Equals(SettingKeys.TripMustStartBefore_Key),
                        cancellationToken: cancellationToken);

                    double maxStartTime = double.Parse(pickupTimeSetting.Value);

                    // Constraint for starting trips
                    if ((bookingDetail.PickUpDateTime() - updateDto.Time.Value).TotalHours >= maxStartTime)
                    {
                        if (maxStartTime < 1)
                        {
                            throw new ApplicationException("Quá sớm để bắt đầu chuyến đi, chỉ được bắt đầu sớm nhất trước" + maxStartTime * 60 + " phút! Vui lòng thử lại sau.");

                        }
                        throw new ApplicationException("Quá sớm để bắt đầu chuyến đi, chỉ được bắt đầu sớm nhất trước " + maxStartTime + " tiếng! Vui lòng thử lại sau.");

                    }
                }
                IEnumerable<Report> reports = await work.Reports
                .GetAllAsync(query => query.Where(
                    r => r.BookingDetailId.Equals(updateDto.BookingDetailId)
                        && (r.Type == ReportType.BOOKER_NOT_COMING ||
                         r.Type == ReportType.DRIVER_NOT_COMING)),
                    cancellationToken: cancellationToken);

                if (reports.Any())
                {
                    throw new ApplicationException("Chuyến đi đã bị báo cáo! Không thể cập nhật chuyến đi");
                }
            }

            if (!bookingDetail.DriverId.HasValue)
            {
                throw new ApplicationException("Chuyến đi chưa được cấu hình tài xế!!");
            }

            if (!IdentityUtilities.IsStaff() && !IdentityUtilities.IsAdmin()
              && !IdentityUtilities.GetCurrentUserId().Equals(bookingDetail.DriverId.Value))
            {
                throw new AccessDeniedException("Bạn không thể thực hiện hành động này!!");
            }

            Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);

            string title = "";
            string description = "";

            string message = "";

            switch (updateDto.Status)
            {
                case BookingDetailStatus.GOING_TO_PICKUP:
                    //bookingDetail.GoingTime = updateDto.Time;
                    if (bookingDetail.Status != BookingDetailStatus.ASSIGNED)
                    {
                        throw new ApplicationException("Trạng thái chuyến đi không hợp lệ!!");
                    }
                    Station startStationGoing = await work.Stations.GetAsync(
                        bookingDetail.StartStationId, cancellationToken: cancellationToken);

                    title = "Tài xế đang bắt đầu đi đến điểm đón - " + startStationGoing.Name;
                    description = "Hãy chắc chắn rằng bạn lên đúng xe nhé!";
                    message = "(Tin nhắn tự động) Tài xế của bạn đang bắt đầu di chuyển đến điểm đón - " + startStationGoing.Name +
                        ". Hãy chắc chắn rằng bạn lên đúng xe nhé!";
                    break;
                case BookingDetailStatus.ARRIVE_AT_PICKUP:
                    if (bookingDetail.Status != BookingDetailStatus.GOING_TO_PICKUP)
                    {
                        throw new ApplicationException("Trạng thái chuyến đi không hợp lệ!!");
                    }
                    bookingDetail.ArriveAtPickupTime = updateDto.Time;

                    Station startStation = await work.Stations.GetAsync(
                        bookingDetail.StartStationId, cancellationToken: cancellationToken);

                    title = "Tài xế đã đến điểm đón - " + startStation.Name;
                    description = "Hãy chắc chắn rằng bạn lên đúng xe nhé!";
                    message = "(Tin nhắn tự động) Tài xế đã đến điểm đón - " + startStation.Name +
                        " - và sẽ đợi bạn trong vòng 5 phút. Vui lòng di chuyến đến điểm đón và " +
                        "hãy chắc chắn rằng bạn lên đúng xe nhé!";
                    break;
                case BookingDetailStatus.GOING_TO_DROPOFF:
                    if (bookingDetail.Status != BookingDetailStatus.ARRIVE_AT_PICKUP)
                    {
                        throw new ApplicationException("Trạng thái chuyến đi không hợp lệ!!");
                    }
                    bookingDetail.PickupTime = updateDto.Time;

                    title = "Chuyến đi của bạn đã được bắt đầu!";
                    description = "Chúc bạn có một chuyến đi an toàn và vui vẻ nhé!";
                    message = "(Tin nhắn tự động) Tài xế đã đón bạn thành công. Chúc bạn có một chuyến đi " +
                        "an toàn và vui vẻ nhé!";
                    break;
                case BookingDetailStatus.ARRIVE_AT_DROPOFF:
                    if (bookingDetail.Status != BookingDetailStatus.GOING_TO_DROPOFF)
                    {
                        throw new ApplicationException("Trạng thái chuyến đi không hợp lệ!!");
                    }
                    bookingDetail.DropoffTime = updateDto.Time;

                    Station startStationDropOff = await work.Stations.GetAsync(
                        bookingDetail.StartStationId, cancellationToken: cancellationToken);

                    Station endStation = await work.Stations.GetAsync(
                        bookingDetail.EndStationId, cancellationToken: cancellationToken);
                    title = "Chuyến đi của bạn đã hoàn thành!";
                    description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStationDropOff.Name} đến {endStation.Name}";
                    break;
            }

            bookingDetail.Status = updateDto.Status;

            //NotificationCreateModel notification = new NotificationCreateModel()
            //{
            //    Type = NotificationType.SPECIFIC_USER,
            //    UserId = booking.CustomerId,
            //    Title = title,
            //    Description = description
            //};

            await work.BookingDetails.UpdateAsync(bookingDetail);
            await work.SaveChangesAsync(cancellationToken);



            // Send notification to Customer and Driver
            User customer = await work.Users.GetAsync(booking.CustomerId,
                cancellationToken: cancellationToken);

            string? customerFcm = customer.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.BookingDetail },
                { "bookingDetailId", bookingDetail.Id.ToString() },
                {"status", bookingDetail.Status.ToString() }
            };

            User driver = await work.Users.GetAsync(
                bookingDetail.DriverId.Value, cancellationToken: cancellationToken);
            string? driverFcm = driver.FcmToken;

            // Send to driver
            if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
            {
                if (updateDto.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
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


                //await FirebaseUtilities.SendNotificationToDeviceAsync(customerFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    customerNotification, customerFcm, dataToSend, cancellationToken);

                if (bookingDetail.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF
                    || bookingDetail.Status == BookingDetailStatus.COMPLETED)
                {
                    // Notification to request rating
                    NotificationCreateModel requestRatingNotification = new NotificationCreateModel()
                    {
                        UserId = customer.Id,
                        Type = NotificationType.SPECIFIC_USER,
                        Title = "Chuyến đi của bạn như thế nào?",
                        Description = "Dành ra 5 phút để đánh giá chuyến đi và tài xế của bạn nhé!",

                    };

                    await notificationServices.CreateFirebaseNotificationAsync(
                       requestRatingNotification, customerFcm, dataToSend, cancellationToken);
                }

            }

            // Send message
            if (!string.IsNullOrEmpty(message))
            {
                await FirestoreUtilities.DbInstance.SendMessageAsync(
               bookingDetail.Id, new FirestoreMessage
               {
                   Id = Guid.NewGuid(),
                   SentBy = driver.Id,
                   SentTo = customer.Id,
                   Text = message,
                   User = new FirestoreMessageUser
                   {
                       Id = driver.Id,
                       Avatar = driver.AvatarUrl ?? "https://raw.githubusercontent.com/ViGo-Routing/ViGo.Driver.V2/main/assets/images/no-image.jpg",
                       Name = driver.Name ?? "Không có dữ liệu"
                   }
               }, cancellationToken);
            }


            return (bookingDetail, customer.Id);
        }

        public async Task<BookingDetail> AssignDriverAsync(BookingDetailAssignDriverModel dto, CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(dto.BookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail is null)
            {
                throw new ApplicationException("Booking Detail không tồn tại!!");
            }

            User driver = await work.Users
                .GetAsync(dto.DriverId, cancellationToken: cancellationToken);
            if (driver == null || driver.Role != UserRole.DRIVER)
            {
                throw new ApplicationException("Tài xế không tồn tại!!");
            }

            Booking booking = await work.Bookings.GetAsync(
                bookingDetail.BookingId, cancellationToken: cancellationToken);

            await CheckDriverSchedules(dto.DriverId, bookingDetail,
                booking, cancellationToken);

            bookingDetail.DriverId = dto.DriverId;
            bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
            bookingDetail.Status = BookingDetailStatus.ASSIGNED;

            await work.BookingDetails.UpdateAsync(bookingDetail);
            await work.SaveChangesAsync(cancellationToken);

            // Send notification to Driver and Customer
            //User driver = await work.Users.GetAsync(driverId, cancellationToken: cancellationToken);
            User customer = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);

            string? driverFcm = driver.FcmToken;
            string? customerFcm = customer.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.BookingDetail },
                { "bookingDetailId", bookingDetail.Id.ToString() },
            };

            Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId,
                cancellationToken: cancellationToken);

            if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
            {
                NotificationCreateModel driverNotification = new NotificationCreateModel()
                {
                    UserId = driver.Id,
                    Title = "Chọn chuyến đi thành công!",
                    Description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStation.Name} đến {endStation.Name}",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(driverFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    driverNotification, driverFcm, dataToSend, cancellationToken);
            }

            if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
            {

                NotificationCreateModel customerNotification = new NotificationCreateModel()
                {
                    UserId = customer.Id,
                    Title = "Chuyến đi của bạn đã có tài xế!",
                    Description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStation.Name} đến {endStation.Name}",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(customerFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    customerNotification, customerFcm, dataToSend, cancellationToken);
            }

            return bookingDetail;
        }

        public async Task<double> CalculateDriverWageAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(bookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail is null)
            {
                throw new ApplicationException("Booking Detail không tồn tại!!");
            }

            FareServices fareServices = new FareServices(work, _logger);
            double driverWage = await fareServices.CalculateDriverWage(bookingDetail.Price.Value, cancellationToken);
            return driverWage;
        }

        public async Task<double> CalculateDriverPickFeeAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(bookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail is null)
            {
                throw new ApplicationException("Booking Detail không tồn tại!!");
            }

            BookingDetailPrice price = await GetBookingDetailPriceAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);

            FareServices fareServices = new FareServices(work, _logger);
            double driverPickingFee = await fareServices.CalculateDriverPickFee(price.BasePrice, cancellationToken);

            return driverPickingFee;
        }

        public async Task<IPagedEnumerable<BookingDetailViewModel>>
            GetDriverAvailableBookingDetailsAsync(Guid driverId,
                Guid? bookingId,
                PaginationParameter pagination, BookingDetailFilterParameters? filters,
                HttpContext context,
                CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                driverId = IdentityUtilities.GetCurrentUserId();
            }

            User driver = await work.Users.GetAsync(driverId, cancellationToken: cancellationToken);
            if (driver == null || driver.Role != UserRole.DRIVER)
            {
                throw new ApplicationException("Tài xế không tồn tại!!!");
            }

            IEnumerable<BookingDetail> unassignedBookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => (bookingId.HasValue ?
                    bd.BookingId.Equals(bookingId.Value) : true) &&
                    !bd.DriverId.HasValue
                    && bd.Status == BookingDetailStatus.PENDING_ASSIGN
                    // Only for future ones
                    /*&& bd.Date >= DateTimeUtilities.GetDateTimeVnNow()*/),
                    cancellationToken: cancellationToken);

            DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();

            unassignedBookingDetails = unassignedBookingDetails.Where(
                bd => (bd.PickUpDateTime() - vnNow).TotalHours >= 3);

            // Get Bookings
            IEnumerable<Guid> bookingIds = unassignedBookingDetails.Select(
                bd => bd.BookingId);
            IEnumerable<Booking> bookings = await work.Bookings.GetAllAsync(
                query => query.Where(b => bookingIds.Contains(b.Id)), cancellationToken: cancellationToken);

            bookings = bookings.Where(b => b.Status == BookingStatus.CONFIRMED);
            bookingIds = bookings.Select(b => b.Id);
            unassignedBookingDetails = unassignedBookingDetails.Where(
                bd => bookingIds.Contains(bd.BookingId));

            // Get Start and End Station
            IEnumerable<Guid> stationIds = unassignedBookingDetails
                .Select(b => b.StartStationId).Concat(unassignedBookingDetails.Select(b => b.EndStationId))
                .Distinct();
            IEnumerable<Station> stations = await work.Stations.GetAllAsync(
                query => query.Where(s => stationIds.Contains(s.Id)), includeDeleted: true,
                cancellationToken: cancellationToken);

            IList<DriverMappingItem> prioritizedBookingDetails = new List<DriverMappingItem>();

            IList<DriverTripsOfDate> driverTrips = await GetDriverSchedulesAsync(driverId, cancellationToken);

            foreach (BookingDetail bookingDetail in unassignedBookingDetails)
            {
                Booking booking = bookings.SingleOrDefault(b => b.Id.Equals(bookingDetail.BookingId));
                TimeSpan bookingDetailEndTime = DateTimeUtilities.CalculateTripEndTime(
                    bookingDetail.CustomerDesiredPickupTime, booking.Duration);

                Station startStation = stations.SingleOrDefault(s => s.Id.Equals(bookingDetail.StartStationId));
                Station endStation = stations.SingleOrDefault(s => s.Id.Equals(bookingDetail.EndStationId));

                if (!driverTrips.Any())
                {
                    // Has not been configured or has no trips
                    prioritizedBookingDetails.Add(new DriverMappingItem(
                        bookingDetail, bookingDetail.CustomerDesiredPickupTime.TotalMinutes));
                }
                else
                {
                    // Has Trips
                    DriverTripsOfDate? tripsOfDate = driverTrips.SingleOrDefault(
                        t => t.Date == DateOnly.FromDateTime(bookingDetail.Date));
                    if (tripsOfDate is null || tripsOfDate.Trips.Count == 0)
                    {
                        // No trips in day
                        prioritizedBookingDetails.Add(new DriverMappingItem(
                            bookingDetail,
                            bookingDetail.CustomerDesiredPickupTime
                                .Add(TimeSpan.FromMinutes(30)).TotalMinutes)); // Less prioritized
                    }
                    else
                    {
                        // Has trips in day
                        DriverTrip addedTrip = new DriverTrip
                        {
                            Id = bookingDetail.Id,
                            BeginTime = bookingDetail.CustomerDesiredPickupTime,
                            EndTime = bookingDetailEndTime,
                            StartLocation = new GoogleMapPoint
                            {
                                Latitude = startStation.Latitude,
                                Longitude = startStation.Longitude
                            },
                            EndLocation = new GoogleMapPoint
                            {
                                Latitude = endStation.Latitude,
                                Longitude = endStation.Longitude
                            }
                        };

                        IEnumerable<DriverTrip> addedTrips = tripsOfDate.Trips.Append(addedTrip).OrderBy(t => t.BeginTime);

                        LinkedList<DriverTrip> addedTripsAsLinkedList = new LinkedList<DriverTrip>(addedTrips);
                        LinkedListNode<DriverTrip> addedTripAsNode = addedTripsAsLinkedList.Find(addedTrip);

                        DriverTrip? previousTrip = addedTripAsNode.Previous?.Value;
                        DriverTrip? nextTrip = addedTripAsNode.Next?.Value;

                        if (previousTrip is null)
                        {
                            if (nextTrip != null)
                            {
                                // Has Next trip
                                if (addedTripAsNode.Value.EndTime < nextTrip.BeginTime)
                                {
                                    // Valid one
                                    await AddToPrioritizedBookingDetailsAsync(prioritizedBookingDetails, bookingDetail,
                                        addedTripAsNode.Value, previousTrip, nextTrip, cancellationToken);
                                }
                            }
                            // Previous and Next cannot be both null
                        }
                        else
                        {
                            // Has Previous trip
                            if (addedTripAsNode.Value.BeginTime > previousTrip.EndTime)
                            {
                                if (nextTrip != null)
                                {
                                    if (addedTripAsNode.Value.EndTime < nextTrip.BeginTime)
                                    {
                                        // Valid one
                                        await AddToPrioritizedBookingDetailsAsync(prioritizedBookingDetails, bookingDetail,
                                            addedTripAsNode.Value, previousTrip, nextTrip, cancellationToken);
                                    }
                                }
                                else
                                {
                                    // Valid one
                                    await AddToPrioritizedBookingDetailsAsync(prioritizedBookingDetails, bookingDetail,
                                        addedTripAsNode.Value, previousTrip, nextTrip, cancellationToken);
                                }
                            }

                        }
                    }
                }
            }

            if (prioritizedBookingDetails.Count == 0)
            {
                // No Available ones
            }
            else
            {
                prioritizedBookingDetails = prioritizedBookingDetails.OrderBy(
                    d => d.BookingDetail.Date)
                    .ThenBy(d => d.PrioritizedPoint).ToList();

            }

            //IEnumerable<BookingDetail> availableBookingDetails 

            if (filters != null)
            {
                prioritizedBookingDetails = (await FilterBookingDetailsAsync(prioritizedBookingDetails,
                filters, cancellationToken)).ToList();
            }

            prioritizedBookingDetails = prioritizedBookingDetails.OrderBy(
                p => p.BookingDetail.Date)
                .ThenBy(p => p.BookingDetail.CustomerDesiredPickupTime)
                .ThenBy(p => p.PrioritizedPoint).ToList();

            int totalCount = prioritizedBookingDetails.Count;
            prioritizedBookingDetails = prioritizedBookingDetails.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data.ToList();

            IEnumerable<Guid> customerRoutineIds = prioritizedBookingDetails.Select(
                p => p.BookingDetail.CustomerRouteRoutineId);
            IEnumerable<RouteRoutine> customerRoutines = await work.RouteRoutines.GetAllAsync(
                query => query.Where(r => customerRoutineIds.Contains(r.Id)), cancellationToken: cancellationToken);

            //prioritizedBookingDetails = prioritizedBookingDetails

            IEnumerable<BookingDetailViewModel> availables = from bookingDetail in prioritizedBookingDetails
                                                             join customerRoutine in customerRoutines
                                                                on bookingDetail.BookingDetail.CustomerRouteRoutineId equals customerRoutine.Id
                                                             join startStation in stations
                                                                on bookingDetail.BookingDetail.StartStationId equals startStation.Id
                                                             join endStation in stations
                                                                 on bookingDetail.BookingDetail.EndStationId equals endStation.Id
                                                             select new BookingDetailViewModel(bookingDetail.BookingDetail,
                                                                 new RouteRoutineViewModel(customerRoutine),
                                                                 new StationViewModel(startStation), new StationViewModel(endStation), null);

            return availables.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize,
                totalCount, context);

        }

        public async Task<BookingDetail> DriverPicksBookingDetailAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            Guid driverId = IdentityUtilities.GetCurrentUserId();
            BookingDetail? bookingDetail = await work.BookingDetails.GetAsync(bookingDetailId,
                cancellationToken: cancellationToken);

            if (bookingDetail is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!!");
            }
            if (bookingDetail.DriverId.HasValue)
            {
                throw new ApplicationException("Chuyến đi đã có tài xế chọn! Vui lòng chọn chuyến khác...");
            }
            Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId, cancellationToken: cancellationToken);
            if (booking.Status != BookingStatus.CONFIRMED)
            {
                throw new ApplicationException("Trạng thái Booking không hợp lệ!!");
            }

            //IList<DriverTripsOfDate> driverTrips = await GetDriverSchedulesAsync(driverId, cancellationToken);

            //if (driverTrips.Count == 0)
            //{
            //    // Has no trips
            //} else
            //{
            //    // Has Trips
            //    DriverTripsOfDate? tripsOfDate = driverTrips.SingleOrDefault(
            //        t => t.Date == DateOnly.FromDateTime(bookingDetail.Date));
            //    if (tripsOfDate is null || tripsOfDate.Trips.Count == 0)
            //    {
            //        // No trips in day

            //    } else
            //    {
            //        // Has trips in day
            //        TimeSpan bookingDetailEndTime = DateTimeUtilities.CalculateTripEndTime(
            //            bookingDetail.CustomerDesiredPickupTime, booking.Duration);

            //        Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId, includeDeleted: true, 
            //            cancellationToken: cancellationToken);
            //        Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId, includeDeleted: true, 
            //            cancellationToken: cancellationToken);

            //        DriverTrip addedTrip = new DriverTrip
            //        {
            //            Id = bookingDetail.Id,
            //            BeginTime = bookingDetail.CustomerDesiredPickupTime,
            //            EndTime = bookingDetailEndTime,
            //            StartLocation = new GoogleMapPoint
            //            {
            //                Latitude = startStation.Latitude,
            //                Longitude = startStation.Longitude
            //            },
            //            EndLocation = new GoogleMapPoint
            //            {
            //                Latitude = endStation.Latitude,
            //                Longitude = endStation.Longitude
            //            }
            //        };

            //        IEnumerable<DriverTrip> addedTrips = tripsOfDate.Trips.Append(addedTrip).OrderBy(t => t.BeginTime);

            //        LinkedList<DriverTrip> addedTripsAsLinkedList = new LinkedList<DriverTrip>(addedTrips);
            //        LinkedListNode<DriverTrip> addedTripAsNode = addedTripsAsLinkedList.Find(addedTrip);

            //        DriverTrip? previousTrip = addedTripAsNode.Previous?.Value;
            //        DriverTrip? nextTrip = addedTripAsNode.Next?.Value;

            //        if (previousTrip != null)
            //        {
            //            if (addedTripAsNode.Value.BeginTime <= previousTrip.EndTime)
            //            {
            //                // Invalid one
            //                throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
            //                    $"Bạn đang chọn chuyến đi có thời gian bắt đầu ({addedTripAsNode.Value.BeginTime}) " +
            //                    $"sớm hơn so với thời gian dự kiến bạn sẽ kết thúc một chuyến đi bạn đã chọn trước đó ({previousTrip.EndTime})");
            //            }
            //        }

            //        if (nextTrip != null)
            //        {
            //            // Has Next trip
            //            if (addedTripAsNode.Value.EndTime >= nextTrip.BeginTime)
            //            {
            //                // Invalid one
            //                throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
            //                    $"Bạn đang chọn chuyến đi có thời gian kết thúc dự kiến ({addedTripAsNode.Value.EndTime}) " +
            //                    $"trễ hơn so với thời gian bạn phải bắt đầu một chuyến đi bạn đã chọn trước đó ({nextTrip.BeginTime})");
            //            }
            //        }
            //    }
            //}
            await CheckDriverSchedules(driverId, bookingDetail,
                booking, cancellationToken);

            // Pick Fee
            Wallet driverWallet = await work.Wallets.GetAsync(
                w => w.UserId.Equals(driverId), cancellationToken: cancellationToken);

            Wallet systemWallet = await work.Wallets.GetAsync(
                w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);

            BookingDetailPrice price = await GetBookingDetailPriceAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);

            FareServices fareServices = new FareServices(work, _logger);
            double pickingFee = await fareServices.CalculateDriverPickFee(price.BasePrice, cancellationToken);

            if (driverWallet.Balance < pickingFee)
            {
                throw new ApplicationException($"Số dư ví không đủ để thực hiện chọn chuyến đi ({driverWallet.Balance.VndFormat()})!");
            }

            WalletTransaction pickingTransaction = new WalletTransaction
            {
                WalletId = driverWallet.Id,
                Amount = pickingFee,
                PaymentMethod = PaymentMethod.WALLET,
                Status = WalletTransactionStatus.SUCCESSFULL,
                Type = WalletTransactionType.TRIP_PICK,
                BookingDetailId = bookingDetail.Id,
            };
            WalletTransaction systemTransaction = new WalletTransaction
            {
                WalletId = systemWallet.Id,
                Amount = pickingFee,
                PaymentMethod = PaymentMethod.WALLET,
                Status = WalletTransactionStatus.SUCCESSFULL,
                Type = WalletTransactionType.TRIP_PICK,
                BookingDetailId = bookingDetail.Id
            };

            driverWallet.Balance -= pickingFee;
            systemWallet.Balance += pickingFee;

            await work.WalletTransactions.InsertAsync(pickingTransaction, cancellationToken: cancellationToken);
            await work.WalletTransactions.InsertAsync(systemTransaction, cancellationToken: cancellationToken);
            await work.Wallets.UpdateAsync(driverWallet);
            await work.Wallets.UpdateAsync(systemWallet);

            bookingDetail.DriverId = driverId;
            bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
            bookingDetail.Status = BookingDetailStatus.ASSIGNED;

            await work.BookingDetails.UpdateAsync(bookingDetail);

            // Check for pending report
            IEnumerable<Report> reports = await work.Reports
                .GetAllAsync(query => query.Where(
                    r => r.BookingDetailId.HasValue
                    && r.BookingDetailId.Equals(bookingDetailId)
                    && r.Status == ReportStatus.PENDING
                    && r.Type == ReportType.DRIVER_CANCEL_TRIP), cancellationToken: cancellationToken);

            if (reports.Any())
            {
                foreach (Report report in reports)
                {
                    report.Status = ReportStatus.PROCESSED;
                    report.ReviewerNote = "Đã được tài xế mới chọn!";

                    await work.Reports.UpdateAsync(report);
                }
            }

            await work.SaveChangesAsync(cancellationToken);

            // Send notification to Driver and Customer
            User driver = await work.Users.GetAsync(driverId, cancellationToken: cancellationToken);
            User customer = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);

            string? driverFcm = driver.FcmToken;
            string? customerFcm = customer.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.BookingDetail },
                { "bookingDetailId", bookingDetail.Id.ToString() },
            };

            Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId,
                cancellationToken: cancellationToken);

            if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
            {
                NotificationCreateModel driverNotification = new NotificationCreateModel()
                {
                    UserId = driverId,
                    Title = "Chọn chuyến đi thành công!",
                    Description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStation.Name} đến {endStation.Name}",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(driverFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    driverNotification, driverFcm, dataToSend, cancellationToken);
            }

            if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
            {

                NotificationCreateModel customerNotification = new NotificationCreateModel()
                {
                    UserId = customer.Id,
                    Title = "Chuyến đi của bạn đã có tài xế!",
                    Description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStation.Name} đến {endStation.Name}",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(customerFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    customerNotification, customerFcm, dataToSend, cancellationToken);
            }

            return bookingDetail;
        }

        public async Task<PickBookingDetailsResponse> DriverPicksBookingDetailsAsync(IEnumerable<Guid> bookingDetailIds,
            CancellationToken cancellationToken)
        {
            if (!bookingDetailIds.Any())
            {
                throw new ApplicationException("Không có chuyến đi nào để chọn!");
            }

            Guid driverId = IdentityUtilities.GetCurrentUserId();

            IEnumerable<BookingDetail> bookingDetails = new List<BookingDetail>();
            foreach (Guid bookingDetailId in bookingDetailIds)
            {
                BookingDetail? bookingDetail = await work.BookingDetails.GetAsync(bookingDetailId,
                cancellationToken: cancellationToken);

                if (bookingDetail is null)
                {
                    throw new ApplicationException("Chuyến đi không tồn tại!!!");
                }
                if (bookingDetail.DriverId.HasValue)
                {
                    throw new ApplicationException($"Chuyến đi vào lúc {bookingDetail.PickUpDateTimeString()} đã có tài xế chọn! Vui lòng chọn chuyến khác...");
                }

                bookingDetails = bookingDetails.Append(bookingDetail);
            }

            BookingDetail firstDetail = bookingDetails.First();
            if (!bookingDetails.All(d => d.BookingId.Equals(firstDetail.BookingId)))
            {
                // They are all from the same booking
                throw new ApplicationException("Các chuyến đi phải thuộc cùng một hành trình!!");
            }

            Booking booking = await work.Bookings.GetAsync(firstDetail.BookingId,
                cancellationToken: cancellationToken);
            if (booking.Status != BookingStatus.CONFIRMED)
            {
                throw new ApplicationException("Trạng thái Booking không hợp lệ!!");
            }

            IEnumerable<BookingDetail> errorBookingDetails = await CheckDriverSchedules(driverId, bookingDetails,
                booking, cancellationToken);

            if (errorBookingDetails.Any())
            {
                IEnumerable<Guid> errorBookingDetailIds = errorBookingDetails.Select(d => d.Id);
                bookingDetails = bookingDetails.Where(d => !errorBookingDetailIds.Contains(d.Id));
            }

            // Pick Fee
            Wallet driverWallet = await work.Wallets.GetAsync(
                w => w.UserId.Equals(driverId), cancellationToken: cancellationToken);

            Wallet systemWallet = await work.Wallets.GetAsync(
                w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);

            BookingDetailPrice price = await GetBookingDetailPriceAsync(booking.Id,
                cancellationToken: cancellationToken);

            FareServices fareServices = new FareServices(work, _logger);
            double pickingFee = await fareServices.CalculateDriverPickFee(price.BasePrice, cancellationToken);

            double totalPickingFee = pickingFee * bookingDetails.Count();

            if (driverWallet.Balance < totalPickingFee)
            {
                throw new ApplicationException($"Số dư ví không đủ để thực hiện chọn chuyến đi ({driverWallet.Balance.VndFormat()})!");
            }

            foreach (BookingDetail bookingDetail in bookingDetails)
            {
                WalletTransaction pickingTransaction = new WalletTransaction
                {
                    WalletId = driverWallet.Id,
                    Amount = pickingFee,
                    PaymentMethod = PaymentMethod.WALLET,
                    Status = WalletTransactionStatus.SUCCESSFULL,
                    Type = WalletTransactionType.TRIP_PICK,
                    BookingDetailId = bookingDetail.Id,
                    //BookingId = booking.Id,
                };

                await work.WalletTransactions.InsertAsync(pickingTransaction, cancellationToken: cancellationToken);

                WalletTransaction systemTransaction = new WalletTransaction
                {
                    WalletId = systemWallet.Id,
                    Amount = pickingFee,
                    PaymentMethod = PaymentMethod.WALLET,
                    Status = WalletTransactionStatus.SUCCESSFULL,
                    Type = WalletTransactionType.TRIP_PICK,
                    BookingDetailId = bookingDetail.Id
                    //BookingId = booking.Id,
                };

                await work.WalletTransactions.InsertAsync(systemTransaction, cancellationToken: cancellationToken);
            }

            driverWallet.Balance -= totalPickingFee;
            systemWallet.Balance += totalPickingFee;

            await work.Wallets.UpdateAsync(driverWallet);
            await work.Wallets.UpdateAsync(systemWallet);

            foreach (BookingDetail bookingDetail in bookingDetails)
            {
                bookingDetail.DriverId = driverId;
                bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
                bookingDetail.Status = BookingDetailStatus.ASSIGNED;

                await work.BookingDetails.UpdateAsync(bookingDetail);

                // Check for pending report
                IEnumerable<Report> reports = await work.Reports
                    .GetAllAsync(query => query.Where(
                        r => r.BookingDetailId.HasValue
                        && r.BookingDetailId.Equals(bookingDetail.Id)
                        && r.Status == ReportStatus.PENDING
                        && r.Type == ReportType.DRIVER_CANCEL_TRIP), cancellationToken: cancellationToken);

                if (reports.Any())
                {
                    foreach (Report report in reports)
                    {
                        report.Status = ReportStatus.PROCESSED;
                        report.ReviewerNote = "Đã được tài xế mới chọn!";

                        await work.Reports.UpdateAsync(report);
                    }
                }
            }

            await work.SaveChangesAsync(cancellationToken);

            // Send notification to Driver and Customer
            User driver = await work.Users.GetAsync(driverId, cancellationToken: cancellationToken);
            User customer = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);

            string? driverFcm = driver.FcmToken;
            string? customerFcm = customer.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.Booking },
                { "bookingId", booking.Id.ToString() },
            };

            Station startStation = await work.Stations.GetAsync(firstDetail.StartStationId,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(firstDetail.EndStationId,
                cancellationToken: cancellationToken);

            if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
            {
                NotificationCreateModel driverNotification = new NotificationCreateModel()
                {
                    UserId = driverId,
                    Title = "Chọn các chuyến đi thành công!",
                    Description = $"Từ " +
                                $"{startStation.Name} đến {endStation.Name} với {bookingDetails.Count()} chuyến đi",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(driverFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    driverNotification, driverFcm, dataToSend, cancellationToken);
            }

            if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
            {

                NotificationCreateModel customerNotification = new NotificationCreateModel()
                {
                    UserId = customer.Id,
                    Title = "Các chuyến đi của bạn đã có tài xế!",
                    Description = $"Từ " +
                                $"{startStation.Name} đến {endStation.Name} với {bookingDetails.Count()} chuyến đi",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(customerFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    customerNotification, customerFcm, dataToSend, cancellationToken);
            }

            PickBookingDetailsResponse response = new PickBookingDetailsResponse()
            {
                SuccessBookingDetailIds = bookingDetails.Select(d => d.Id),
                ErrorMessage = string.Empty,
                DriverId = driverId
            };
            if (errorBookingDetails.Any())
            {
                response.ErrorMessage = "Các chuyến đi không thể chọn do bị trùng lịch trình: ";
                foreach (BookingDetail bookingDetail in errorBookingDetails)
                {
                    response.ErrorMessage += $"{bookingDetail.PickUpDateTimeString()}, ";
                }
                response.ErrorMessage.Substring(0, response.ErrorMessage.Length - 2);
            }
            return response;
        }

        public async Task<(BookingDetail, Guid?, bool, bool, Guid?)> CancelBookingDetailAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            BookingDetail? bookingDetail = await work.BookingDetails
                .GetAsync(bookingDetailId, cancellationToken: cancellationToken);

            if (bookingDetail is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!!");
            }

            bool isInWeek = false;

            User? cancelledUser = null;

            bool isDriverPaid = false;
            Guid? customerId = null;

            if (bookingDetail.Status == BookingDetailStatus.CANCELLED)
            {
                return (bookingDetail, null, false, false, null);
            }

            Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);

            if (!IdentityUtilities.IsAdmin())
            {
                Guid currentId = IdentityUtilities.GetCurrentUserId();
                if (!currentId.Equals(booking.CustomerId) &&
                    (bookingDetail.DriverId.HasValue &&
                    !currentId.Equals(bookingDetail.DriverId.Value)))
                {
                    // Not the accessible user
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!");
                }

                if (currentId.Equals(booking.CustomerId))
                {
                    // Customer cancels the bookingdetail
                    cancelledUser = await work.Users.GetAsync(booking.CustomerId,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    if (bookingDetail.DriverId.HasValue &&
                        currentId.Equals(bookingDetail.DriverId.Value))
                    {
                        // Driver cancels the bookingdetail
                        cancelledUser = await work.Users.GetAsync(
                            bookingDetail.DriverId.Value, cancellationToken: cancellationToken);
                    }
                }
            }

            DateTime now = DateTimeUtilities.GetDateTimeVnNow();
            DateTime pickupDateTime = DateOnly.FromDateTime(bookingDetail.Date)
                .ToDateTime(TimeOnly.FromTimeSpan(bookingDetail.CustomerDesiredPickupTime));

            // TODO Code for Cancelling Policy
            if (now > pickupDateTime)
            {
                throw new ApplicationException("Chuyến đi trong quá khứ, không thể thực hiện hủy chuyến đi!");
            }

            Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId,
                cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId,
                cancellationToken: cancellationToken);

            if (cancelledUser != null)
            {
                if (bookingDetail.Date.IsInCurrentWeek())
                {
                    if (cancelledUser.WeeklyCanceledTripRate >= 3)
                    {
                        throw new ApplicationException("Số chuyến đi được phép hủy có lịch trình trong tuần này của bạn " +
                            "đã đạt giới hạn (3 chuyến đi). Bạn không thể hủy thêm chuyến đi nào có lịch trình " +
                            "trong tuần này nữa!");
                    }
                    isInWeek = true;
                }

                Wallet systemWallet = await work.Wallets.GetAsync(
                     w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);

                if (cancelledUser.Role == UserRole.CUSTOMER)
                {
                    // Customer
                    double chargeFee = 0;

                    if (!bookingDetail.DriverId.HasValue)
                    {
                        // BookingDetail has not been selected by a Driver
                        // No fee
                        chargeFee = 0;
                    }
                    else
                    {
                        TimeSpan difference = pickupDateTime - now;

                        if (difference.TotalHours >= 6)
                        {
                            // >= 6 hours
                            // No extra fee
                            chargeFee = 0;
                        }
                        else if (difference.TotalMinutes >= 45)
                        {
                            // >= 1 hour but < 6h
                            // Been selected
                            // 10% extra fee
                            chargeFee = 0.1;
                        }
                        else
                        {
                            // 100% fee
                            // No fee
                            chargeFee = 1;
                        }
                    }

                    //double chargeFeeAmount = bookingDetail.PriceAfterDiscount.Value * chargeFee;
                    //chargeFeeAmount = FareUtilities.RoundToThousands(chargeFeeAmount);
                    double customerRefundAmount = bookingDetail.Price.Value * (1 - chargeFee);
                    customerRefundAmount = FareUtilities.RoundToThousands(customerRefundAmount);

                    if (customerRefundAmount > 0)
                    {
                        Wallet customerWallet = await work.Wallets.GetAsync(
                            w => w.UserId.Equals(cancelledUser.Id),
                            cancellationToken: cancellationToken);

                        WalletTransaction walletTransaction = new WalletTransaction
                        {
                            WalletId = customerWallet.Id,
                            BookingDetailId = bookingDetail.Id,
                            Amount = customerRefundAmount,
                            Type = WalletTransactionType.CANCEL_REFUND,
                            Status = WalletTransactionStatus.SUCCESSFULL,
                            PaymentMethod = PaymentMethod.WALLET
                        };

                        await work.WalletTransactions.InsertAsync(walletTransaction,
                            cancellationToken: cancellationToken);
                        customerWallet.Balance += customerRefundAmount;

                        //if (wallet.Balance >= chargeFeeAmount)
                        //{
                        //    // Able to be substracted the amount
                        //    wallet.Balance -= chargeFeeAmount;

                        //walletTransaction.Status = WalletTransactionStatus.SUCCESSFULL;


                        WalletTransaction systemTransaction = new WalletTransaction
                        {
                            WalletId = systemWallet.Id,
                            BookingDetailId = bookingDetail.Id,
                            Amount = customerRefundAmount,
                            Type = WalletTransactionType.CANCEL_REFUND,
                            Status = WalletTransactionStatus.SUCCESSFULL,
                            PaymentMethod = PaymentMethod.WALLET
                        };

                        //systemWallet.Balance += chargeFeeAmount;
                        systemWallet.Balance -= customerRefundAmount;

                        await work.WalletTransactions.InsertAsync(systemTransaction,
                        cancellationToken: cancellationToken);
                        await work.Wallets.UpdateAsync(systemWallet);

                        await work.Wallets.UpdateAsync(customerWallet);
                        //}

                    }

                    // Refund to Driver
                    if (bookingDetail.DriverId.HasValue)
                    {
                        Wallet driverWallet = await work.Wallets.GetAsync(
                            w => w.UserId.Equals(bookingDetail.DriverId.Value),
                            cancellationToken: cancellationToken);

                        if (chargeFee == 1)
                        {
                            // Trip was considered completed, Driver gets paid for the whole trip
                            isDriverPaid = true;
                            customerId = cancelledUser.Id;

                        }
                        else
                        {
                            IEnumerable<WalletTransaction> driverPickDetailTransactions = await work.WalletTransactions
                                .GetAllAsync(query => query.Where(t => t.WalletId.Equals(driverWallet.Id)
                                && t.BookingDetailId.Equals(bookingDetail.Id)
                                && t.Type == WalletTransactionType.TRIP_PICK), cancellationToken: cancellationToken);

                            driverPickDetailTransactions = driverPickDetailTransactions.OrderByDescending(
                                t => t.CreatedTime);
                            WalletTransaction driverPickDetailTransaction = driverPickDetailTransactions.First();

                            double pickFee = driverPickDetailTransaction.Amount;
                            WalletTransaction driverPickRefundTransaction = new WalletTransaction()
                            {
                                WalletId = driverWallet.Id,
                                Amount = pickFee,
                                Type = WalletTransactionType.TRIP_PICK_REFUND,
                                BookingDetailId = bookingDetail.Id,
                                Status = WalletTransactionStatus.SUCCESSFULL,
                                PaymentMethod = PaymentMethod.WALLET
                            };

                            await work.WalletTransactions.InsertAsync(driverPickRefundTransaction,
                                cancellationToken: cancellationToken);

                            driverWallet.Balance += pickFee;
                            await work.Wallets.UpdateAsync(driverWallet);

                            WalletTransaction systemRefundTransaction = new WalletTransaction
                            {
                                WalletId = systemWallet.Id,
                                BookingDetailId = bookingDetail.Id,
                                Amount = pickFee,
                                Type = WalletTransactionType.TRIP_PICK_REFUND,
                                Status = WalletTransactionStatus.SUCCESSFULL,
                                PaymentMethod = PaymentMethod.WALLET
                            };

                            //systemWallet.Balance += chargeFeeAmount;
                            systemWallet.Balance -= pickFee;

                            await work.WalletTransactions.InsertAsync(systemRefundTransaction,
                                cancellationToken: cancellationToken);
                            await work.Wallets.UpdateAsync(systemWallet);
                        }
                    }

                    bookingDetail.Status = BookingDetailStatus.CANCELLED;

                    bookingDetail.CanceledUserId = IdentityUtilities.GetCurrentUserId();

                }
                else if (cancelledUser.Role == UserRole.DRIVER)
                {
                    // Driver
                    double chargeFee = 0;

                    if (!bookingDetail.DriverId.HasValue)
                    {
                        // BookingDetail has not been selected by a Driver
                        // No fee
                        chargeFee = 0;
                    }
                    else
                    {
                        TimeSpan difference = pickupDateTime - now;

                        if (difference.TotalHours >= 24)
                        {
                            // >= 6 hours
                            // No extra fee
                            chargeFee = 0;
                        }
                        else if (difference.TotalHours >= 6)
                        {
                            // >= 1 hour but < 6h
                            // Been selected
                            // 10% extra fee
                            chargeFee = 0.25;
                        }
                        else if (difference.TotalHours >= 2)
                        {
                            // >= 1 hour but < 6h
                            // Been selected
                            // 10% extra fee
                            chargeFee = 0.5;
                        }
                        else
                        {
                            // 100% fee
                            // No fee
                            chargeFee = 1;
                        }
                    }

                    //double chargeFeeAmount = bookingDetail.PriceAfterDiscount.Value * chargeFee;
                    //chargeFeeAmount = FareUtilities.RoundToThousands(chargeFeeAmount);
                    //double driverRefundAmount = bookingDetail.Price.Value * (1 - chargeFee);
                    //customerRefundAmount = FareUtilities.RoundToThousands(customerRefundAmount);

                    // Refund to Driver
                    Wallet driverWallet = await work.Wallets.GetAsync(
                        w => w.UserId.Equals(bookingDetail.DriverId.Value),
                        cancellationToken: cancellationToken);

                    WalletTransaction driverPickDetailTransaction = await work.WalletTransactions
                        .GetAsync(t => t.WalletId.Equals(driverWallet.Id)
                        && t.BookingDetailId.Equals(bookingDetail.Id)
                        && t.Type == WalletTransactionType.TRIP_PICK, cancellationToken: cancellationToken);

                    double pickFee = driverPickDetailTransaction.Amount;
                    double refundFee = FareUtilities.RoundToThousands(pickFee * (1 - chargeFee));
                    if (refundFee > 0)
                    {
                        WalletTransaction driverPickRefundTransaction = new WalletTransaction()
                        {
                            WalletId = driverWallet.Id,
                            Amount = refundFee,
                            Type = WalletTransactionType.TRIP_PICK_REFUND,
                            BookingDetailId = bookingDetail.Id,
                            Status = WalletTransactionStatus.SUCCESSFULL,
                            PaymentMethod = PaymentMethod.WALLET
                        };

                        await work.WalletTransactions.InsertAsync(driverPickRefundTransaction,
                            cancellationToken: cancellationToken);

                        driverWallet.Balance += pickFee;
                        await work.Wallets.UpdateAsync(driverWallet);

                        WalletTransaction systemRefundTransaction = new WalletTransaction
                        {
                            WalletId = systemWallet.Id,
                            BookingDetailId = bookingDetail.Id,
                            Amount = refundFee,
                            Type = WalletTransactionType.TRIP_PICK_REFUND,
                            Status = WalletTransactionStatus.SUCCESSFULL,
                            PaymentMethod = PaymentMethod.WALLET
                        };

                        //systemWallet.Balance += chargeFeeAmount;
                        systemWallet.Balance -= pickFee;

                        await work.WalletTransactions.InsertAsync(systemRefundTransaction,
                            cancellationToken: cancellationToken);
                        await work.Wallets.UpdateAsync(systemWallet);
                    }

                    // Create Report to admin for assigning to the trip
                    Report report = new Report()
                    {
                        BookingDetailId = bookingDetail.Id,
                        Title = "Tài xế hủy chuyến đi",
                        Content = "Tài xế đã hủy chuyến đi, chuyến đi cần tài xế mới!\n" +
                        "Chuyến đi bị hủy: " + $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStation.Name} đến {endStation.Name}",
                        UserId = bookingDetail.DriverId.Value,
                        Type = ReportType.DRIVER_CANCEL_TRIP,
                        Status = ReportStatus.PENDING,

                    };

                    await work.Reports.InsertAsync(report, cancellationToken: cancellationToken);

                    bookingDetail.Status = BookingDetailStatus.PENDING_ASSIGN;
                    bookingDetail.DriverId = null;

                    bookingDetail.CanceledUserId = IdentityUtilities.GetCurrentUserId();
                }
            }

            // Trigger Background Task for Calculating Cancel Rate

            await work.BookingDetails.UpdateAsync(bookingDetail);

            await work.SaveChangesAsync(cancellationToken);

            // Send notification to Driver and Customer
            User? driver = null;
            if (bookingDetail.DriverId.HasValue)
            {
                driver = await work.Users.GetAsync(bookingDetail.DriverId.Value,
                    cancellationToken: cancellationToken);
            }
            else if (cancelledUser != null && cancelledUser.Role == UserRole.DRIVER)
            {
                driver = cancelledUser;
            }

            User customer = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);

            string? driverFcm = driver?.FcmToken;
            string? customerFcm = customer.FcmToken;


            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.BookingDetail },
                { "bookingDetailId", bookingDetail.Id.ToString() },
            };



            if (driver != null
                && driverFcm != null && !string.IsNullOrEmpty(driverFcm))
            {
                NotificationCreateModel driverNotification = new NotificationCreateModel()
                {
                    UserId = driver.Id,
                    Title = "Chuyến đi đã bị hủy!",
                    Description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStation.Name} đến {endStation.Name}",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(driverFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    driverNotification, driverFcm, dataToSend, cancellationToken);
            }

            if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
            {

                NotificationCreateModel customerNotification = new NotificationCreateModel()
                {
                    UserId = customer.Id,
                    Title = "Chuyến đi đã bị hủy!",
                    Description = $"{bookingDetail.PickUpDateTimeString()}, từ " +
                                $"{startStation.Name} đến {endStation.Name}",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(customerFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    customerNotification, customerFcm, dataToSend, cancellationToken);
            }

            return (bookingDetail, cancelledUser?.Id, isInWeek, isDriverPaid, customerId);
        }

        public async Task<double> CalculateCancelBookingDetailFeeAsync(
            Guid bookingDetailId, CancellationToken cancellationToken)
        {
            BookingDetail? bookingDetail = await work.BookingDetails
                .GetAsync(bookingDetailId, cancellationToken: cancellationToken);

            if (bookingDetail is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!!");
            }

            if (bookingDetail.Status == BookingDetailStatus.CANCELLED)
            {
                return 0;
            }

            Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);

            if (!IdentityUtilities.IsAdmin())
            {
                Guid currentId = IdentityUtilities.GetCurrentUserId();
                if (!currentId.Equals(booking.CustomerId) &&
                    (bookingDetail.DriverId.HasValue &&
                    !currentId.Equals(bookingDetail.DriverId.Value)))
                {
                    // Not the accessible user
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!");
                }
            }

            DateTime now = DateTimeUtilities.GetDateTimeVnNow();
            DateTime pickupDateTime = DateOnly.FromDateTime(bookingDetail.Date)
                .ToDateTime(TimeOnly.FromTimeSpan(bookingDetail.CustomerDesiredPickupTime));

            // TODO Code for Cancelling Policy
            if (now > pickupDateTime)
            {
                throw new ApplicationException("Chuyến đi trong quá khứ, không thể thực hiện hủy chuyến đi!");
            }

            if (IdentityUtilities.GetCurrentRole() == UserRole.CUSTOMER)
            {
                // Customer
                double chargeFee = 0;

                if (!bookingDetail.DriverId.HasValue)
                {
                    // BookingDetail has not been selected by a Driver
                    // No fee
                    chargeFee = 0;
                }
                else
                {
                    TimeSpan difference = pickupDateTime - now;

                    if (difference.TotalHours >= 6)
                    {
                        // >= 6 hours
                        // No extra fee
                        chargeFee = 0;
                    }
                    else if (difference.TotalMinutes >= 45)
                    {
                        // >= 1 hour but < 6h
                        // Been selected
                        // 10% extra fee
                        chargeFee = 0.1;
                    }
                    else
                    {
                        // 100% fee
                        // No fee
                        chargeFee = 1;
                    }
                }

                //double chargeFeeAmount = bookingDetail.PriceAfterDiscount.Value * chargeFee;
                //chargeFeeAmount = FareUtilities.RoundToThousands(chargeFeeAmount);
                //double customerRefundAmount = bookingDetail.Price.Value * (1 - chargeFee);
                return FareUtilities.RoundToThousands(bookingDetail.Price.Value * chargeFee);

            }
            else if (IdentityUtilities.GetCurrentRole() == UserRole.DRIVER)
            {
                // Driver
                double chargeFee = 0;

                if (!bookingDetail.DriverId.HasValue)
                {
                    // BookingDetail has not been selected by a Driver
                    // No fee
                    chargeFee = 0;
                }
                else
                {
                    TimeSpan difference = pickupDateTime - now;

                    if (difference.TotalHours >= 24)
                    {
                        // >= 6 hours
                        // No extra fee
                        chargeFee = 0;
                    }
                    else if (difference.TotalHours >= 6)
                    {
                        // >= 1 hour but < 6h
                        // Been selected
                        // 10% extra fee
                        chargeFee = 0.25;
                    }
                    else if (difference.TotalHours >= 2)
                    {
                        // >= 1 hour but < 6h
                        // Been selected
                        // 10% extra fee
                        chargeFee = 0.5;
                    }
                    else
                    {
                        // 100% fee
                        // No fee
                        chargeFee = 1;
                    }
                }

                //double chargeFeeAmount = bookingDetail.PriceAfterDiscount.Value * chargeFee;
                //chargeFeeAmount = FareUtilities.RoundToThousands(chargeFeeAmount);
                //double driverRefundAmount = bookingDetail.Price.Value * (1 - chargeFee);
                //customerRefundAmount = FareUtilities.RoundToThousands(customerRefundAmount);

                // Refund to Driver
                Wallet driverWallet = await work.Wallets.GetAsync(
                    w => w.UserId.Equals(bookingDetail.DriverId.Value),
                    cancellationToken: cancellationToken);

                WalletTransaction driverPickDetailTransaction = await work.WalletTransactions
                    .GetAsync(t => t.WalletId.Equals(driverWallet.Id)
                    && t.BookingDetailId.Equals(bookingDetail.Id)
                    && t.Type == WalletTransactionType.TRIP_PICK, cancellationToken: cancellationToken);

                if (driverPickDetailTransaction != null)
                {
                    double pickFee = driverPickDetailTransaction.Amount;
                    //double refundFee = FareUtilities.RoundToThousands(pickFee * (1 - chargeFee));
                    return FareUtilities.RoundToThousands(pickFee * chargeFee);
                }
            }
            return 0;
        }

        public async Task<(BookingDetailViewModel, Guid)> UserUpdateFeedback(
            Guid id, BookingDetailFeedbackModel feedback,
            CancellationToken cancellationToken)
        {
            var currentBookingDetail = await work.BookingDetails.GetAsync(id,
                cancellationToken: cancellationToken);

            if (currentBookingDetail is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!");
            }
            if (!currentBookingDetail.DriverId.HasValue ||
                (currentBookingDetail.Status != BookingDetailStatus.ARRIVE_AT_DROPOFF
                && currentBookingDetail.Status != BookingDetailStatus.COMPLETED))
            {
                throw new ApplicationException("Trạng thái chuyến đi không hợp lệ để đánh giá!!");
            }

            if (currentBookingDetail.Rate.HasValue)
            {
                throw new ApplicationException("Chuyến đi đã được đánh giá rồi!");
            }

            Guid bookingID = currentBookingDetail.BookingId;
            Booking booking = await work.Bookings.GetAsync(bookingID,
                cancellationToken: cancellationToken);

            if (booking.CustomerId != IdentityUtilities.GetCurrentUserId())
            {
                throw new AccessDeniedException("Bạn chỉ được phép đánh giá chuyến đi của mình thôi!");
            }

            //if (currentBookingDetail == null)
            //{
            //    throw new ApplicationException("Booking Detail ID không tồn tại!");
            //}
            //else
            //{
            if (feedback.Rate <= 0 || feedback.Rate > 5)
            {
                throw new ApplicationException("Đánh giá phải từ 1 đến 5 sao!");
            }
            feedback.Feedback.StringValidate(
                allowEmpty: true,
                minLength: 5,
                minLengthErrorMessage: "Đánh giá phải có từ 5 kí tự trở lên!!",
                maxLength: 500,
                maxLengthErrorMessage: "Đánh giá không được vượt quá 500 kí tự!!");

            currentBookingDetail.Rate = feedback.Rate;
            currentBookingDetail.Feedback = feedback.Feedback;

            //}
            await work.BookingDetails.UpdateAsync(currentBookingDetail);
            await work.SaveChangesAsync(cancellationToken);

            // Send notification to Driver
            User driver = await work.Users.GetAsync(currentBookingDetail.DriverId.Value,
                cancellationToken: cancellationToken);
            string? driverFcm = driver.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.Booking },
                { "bookingDetailId", currentBookingDetail.Id.ToString() },
            };

            if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
            {

                NotificationCreateModel customerNotification = new NotificationCreateModel()
                {
                    UserId = driver.Id,
                    Title = "Bạn có một đánh giá mới!",
                    Description = $"{feedback.Rate}/5 - {feedback.Feedback}",
                    Type = NotificationType.SPECIFIC_USER
                };

                //await FirebaseUtilities.SendNotificationToDeviceAsync(customerFcm, title,
                //                           description, data: dataToSend,
                //                               cancellationToken: cancellationToken);

                //// Send data to mobile application
                //await FirebaseUtilities.SendDataToDeviceAsync(driverFcm, dataToSend, cancellationToken);

                await notificationServices.CreateFirebaseNotificationAsync(
                    customerNotification, driverFcm, dataToSend, cancellationToken);
            }

            BookingDetailViewModel bookingDetailView = new BookingDetailViewModel(currentBookingDetail);
            return (bookingDetailView, driver.Id);

        }

        //public async Task<IPagedEnumerable<BookingDetailViewModel>>

        public async Task<BookingDetailAnalysisModel> GetBookingDetailAnalysisAsync(
            CancellationToken cancellationToken)
        {
            IEnumerable<BookingDetail> bookingDetails;
            UserRole currentRole = IdentityUtilities.GetCurrentRole();

            if (currentRole == UserRole.ADMIN)
            {
                // Admin
                bookingDetails = await work.BookingDetails.GetAllAsync(cancellationToken: cancellationToken);
            }
            else if (currentRole == UserRole.CUSTOMER)
            {
                // Customer
                IEnumerable<Booking> bookings = await work.Bookings.GetAllAsync(query => query.Where(
                    b => b.CustomerId.Equals(IdentityUtilities.GetCurrentUserId())), cancellationToken: cancellationToken);
                IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);
                bookingDetails = await work.BookingDetails.GetAllAsync(
                    query => query.Where(d => bookingIds.Contains(d.BookingId)), cancellationToken: cancellationToken);

            }
            else /*if (IdentityUtilities.GetCurrentRole() == UserRole.DRIVER)*/
            {
                // Driver
                bookingDetails = await work.BookingDetails.GetAllAsync(query => query.Where(
                    d => d.DriverId.HasValue && d.DriverId.Value.Equals(IdentityUtilities.GetCurrentUserId())),
                    cancellationToken: cancellationToken);

            }

            BookingDetailAnalysisModel bookingDetailAnalysis = new BookingDetailAnalysisModel()
            {
                TotalBookingDetails = bookingDetails.Count(),
                TotalCanceledBookingDetails = bookingDetails.Count(d => d.Status == BookingDetailStatus.CANCELLED),
                TotalCompletedBookingDetails = bookingDetails.Count(d => d.Status == BookingDetailStatus.COMPLETED),
            };

            IEnumerable<Guid> canceledUserIds = bookingDetails.Where(
                    d => d.Status == BookingDetailStatus.CANCELLED
                    && d.CanceledUserId.HasValue).Select(d => d.CanceledUserId.Value)
                    .Distinct();
            IEnumerable<User> canceledUsers = await work.Users.GetAllAsync(query => query.Where(
                u => canceledUserIds.Contains(u.Id)), cancellationToken: cancellationToken);

            bookingDetailAnalysis.TotalCanceledByCustomerBookingDetails =
                bookingDetails.Count(d =>
                {
                    if (d.Status == BookingDetailStatus.CANCELLED &&
                    d.CanceledUserId.HasValue)
                    {
                        User canceledUser = canceledUsers.SingleOrDefault(
                            u => u.Id.Equals(d.CanceledUserId.Value));
                        return canceledUser.Role == UserRole.CUSTOMER;
                    }
                    return false;
                });

            bookingDetailAnalysis.TotalCanceledByDriverBookingDetails =
                bookingDetails.Count(d =>
                {
                    if (d.Status == BookingDetailStatus.CANCELLED &&
                    d.CanceledUserId.HasValue)
                    {
                        User canceledUser = canceledUsers.SingleOrDefault(
                            u => u.Id.Equals(d.CanceledUserId.Value));
                        return canceledUser.Role == UserRole.DRIVER;
                    }
                    return false;
                });

            if (currentRole == UserRole.ADMIN
                || currentRole == UserRole.CUSTOMER)
            {
                bookingDetailAnalysis.TotalAssignedBookingDetails =
                    bookingDetails.Count(d => d.DriverId.HasValue);

                bookingDetailAnalysis.TotalUnassignedBookingDetails =
                    bookingDetails.Count(d => !d.DriverId.HasValue);

                bookingDetailAnalysis.TotalPendingPaidBookingDetails =
                    bookingDetails.Count(d => d.Status == BookingDetailStatus.PENDING_PAID);


            }
            else
            {
                // Driver
            }

            return bookingDetailAnalysis;
        }

        public async Task<DriverSchedulesForPickingResponse>
            GetDriverSchedulesForPickingAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            Guid driverId = IdentityUtilities.GetCurrentUserId();

            BookingDetail current = await work.BookingDetails.GetAsync(bookingDetailId, cancellationToken: cancellationToken);
            if (current is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!");
            }

            //RouteRoutine currentRoutine = await work.RouteRoutines
            //    .GetAsync(current.CustomerRouteRoutineId, 
            //    cancellationToken: cancellationToken);
            Station currentStartStation = await work.Stations.GetAsync(current.StartStationId, cancellationToken: cancellationToken);
            Station currentEndStation = await work.Stations.GetAsync(current.EndStationId, cancellationToken: cancellationToken);
            BookingDetailViewModel currentViewModel = new BookingDetailViewModel(current, null,
                new StationViewModel(currentStartStation), new StationViewModel(currentEndStation), null);

            DriverSchedulesForPickingResponse schedules = new DriverSchedulesForPickingResponse(null,
                currentViewModel, null);

            IEnumerable<BookingDetailViewModel> driverTrips = new List<BookingDetailViewModel>();

            DateOnly date = DateOnly.FromDateTime(current.Date);
            IEnumerable<BookingDetail> driverBookingDetails = await
                work.BookingDetails.GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                        bd.DriverId.Value.Equals(driverId)),
                        cancellationToken: cancellationToken);
            driverBookingDetails = driverBookingDetails.Where(bd =>
                DateOnly.FromDateTime(bd.Date).Equals(date))
                .OrderBy(d => d.CustomerDesiredPickupTime);

            //if (driverBookingDetails.Any())
            //{
            //    //IEnumerable<Guid> driverBookingIds = driverBookingDetails.Select(d => d.BookingId).Distinct();
            //    //IEnumerable<Booking> driverBookings = await work.Bookings.GetAllAsync(
            //    //    query => query.Where(b => driverBookingIds.Contains(b.Id)), cancellationToken: cancellationToken);

            //    IEnumerable<Guid> driverStationIds = driverBookingDetails.Select(
            //        b => b.StartStationId).Concat(driverBookingDetails.Select(b => b.EndStationId))
            //        .Distinct();
            //    IEnumerable<Station> driverStations = await work.Stations.GetAllAsync(
            //        query => query.Where(s => driverStationIds.Contains(s.Id)), includeDeleted: true,
            //        cancellationToken: cancellationToken);
            //    //IEnumerable<DateOnly> tripDates = driverBookingDetails.Select(
            //    //    d => DateOnly.FromDateTime(d.Date)).Distinct();

            //    //IEnumerable<BookingDetail> tripsInDate = driverBookingDetails.Where(
            //    //    bd => DateOnly.FromDateTime(bd.Date) == date);
            //    driverTrips = from detail in driverBookingDetails
            //                      //join booking in driverBookings
            //                      //    on detail.BookingId equals booking.Id
            //                  join startStation in driverStations
            //                      on detail.StartStationId equals startStation.Id
            //                  join endStation in driverStations
            //                      on detail.EndStationId equals endStation.Id
            //                  select new BookingDetailViewModel(detail, null, new StationViewModel(startStation), new StationViewModel(endStation), null);

            //    driverTrips = driverTrips.OrderBy(t => t.CustomerDesiredPickupTime);

            //        //driverTrips.Add(new DriverTripsOfDate
            //        //{
            //        //    Date = tripDate,
            //        //    Trips = trips.ToList()
            //        //});
            //    }

            if (driverBookingDetails.Count() == 0)
            {
                // Has no trips
                return schedules;
            }
            else
            {
                IEnumerable<BookingDetail> addedTrips = driverBookingDetails.Append(current)
                    .OrderBy(t => t.CustomerDesiredPickupTime);
                LinkedList<BookingDetail> addedTripsAsLinkedList = new LinkedList<BookingDetail>(addedTrips);
                LinkedListNode<BookingDetail> addedTripAsNode = addedTripsAsLinkedList.Find(current);

                BookingDetail? previousTrip = addedTripAsNode.Previous?.Value;
                BookingDetail? nextTrip = addedTripAsNode.Next?.Value;

                IEnumerable<Guid> driverStationIds = new List<Guid>()
                {
                    current.StartStationId, current.EndStationId,

                };
                if (previousTrip != null)
                {
                    driverStationIds = driverStationIds.Concat(new List<Guid>{
                        previousTrip.StartStationId, previousTrip.EndStationId
                    });

                }
                if (nextTrip != null)
                {
                    driverStationIds = driverStationIds.Concat(new List<Guid>{
                        nextTrip.StartStationId, nextTrip.EndStationId
                    });
                }
                driverStationIds = driverStationIds.Distinct();

                IEnumerable<Station> driverStations = await work.Stations.GetAllAsync(
                    query => query.Where(s => driverStationIds.Contains(s.Id)), includeDeleted: true,
                    cancellationToken: cancellationToken);

                BookingDetailViewModel? previous = null;
                BookingDetailViewModel? next = null;
                if (previousTrip != null)
                {
                    Station previousStartStation = driverStations.Single(
                        s => s.Id.Equals(previousTrip.StartStationId));
                    Station previousEndStation = driverStations.Single(
                        s => s.Id.Equals(previousTrip.EndStationId));
                    previous = new BookingDetailViewModel(previousTrip, null,
                        new StationViewModel(previousStartStation), new StationViewModel(previousEndStation),
                        null);
                }
                if (nextTrip != null)
                {
                    Station nextStartStation = driverStations.Single(
                        s => s.Id.Equals(nextTrip.StartStationId));
                    Station nextEndStation = driverStations.Single(
                        s => s.Id.Equals(nextTrip.EndStationId));
                    next = new BookingDetailViewModel(nextTrip, null,
                        new StationViewModel(nextStartStation), new StationViewModel(nextEndStation),
                        null);
                }
                schedules.PreviousTrip = previous;
                schedules.NextTrip = next;

            }

            return schedules;
        }


        #region Private
        private async Task<IList<DriverTripsOfDate>> GetDriverSchedulesAsync(Guid driverId,
            CancellationToken cancellationToken)
        {
            // Get driver current schedules
            IList<DriverTripsOfDate> driverTrips = new List<DriverTripsOfDate>();
            IEnumerable<BookingDetail> driverBookingDetails = await
                work.BookingDetails.GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                        bd.DriverId.Value.Equals(driverId)
                        && bd.Status != BookingDetailStatus.CANCELLED), cancellationToken: cancellationToken);

            if (driverBookingDetails.Any())
            {
                IEnumerable<Guid> driverBookingIds = driverBookingDetails.Select(d => d.BookingId).Distinct();
                IEnumerable<Booking> driverBookings = await work.Bookings.GetAllAsync(
                    query => query.Where(b => driverBookingIds.Contains(b.Id)), cancellationToken: cancellationToken);

                IEnumerable<Guid> driverStationIds = driverBookingDetails.Select(
                    b => b.StartStationId).Concat(driverBookingDetails.Select(b => b.EndStationId))
                    .Distinct();
                IEnumerable<Station> driverStations = await work.Stations.GetAllAsync(
                    query => query.Where(s => driverStationIds.Contains(s.Id)), includeDeleted: true,
                    cancellationToken: cancellationToken);
                IEnumerable<DateOnly> tripDates = driverBookingDetails.Select(
                    d => DateOnly.FromDateTime(d.Date)).Distinct();
                foreach (DateOnly tripDate in tripDates)
                {
                    IEnumerable<BookingDetail> tripsInDate = driverBookingDetails.Where(
                        bd => DateOnly.FromDateTime(bd.Date) == tripDate);
                    IEnumerable<DriverTrip> trips = from detail in tripsInDate
                                                    join booking in driverBookings
                                                        on detail.BookingId equals booking.Id
                                                    join startStation in driverStations
                                                        on detail.StartStationId equals startStation.Id
                                                    join endStation in driverStations
                                                        on detail.EndStationId equals endStation.Id
                                                    select new DriverTrip()
                                                    {
                                                        Id = detail.Id,
                                                        BeginTime = detail.CustomerDesiredPickupTime,
                                                        EndTime = DateTimeUtilities.CalculateTripEndTime(detail.CustomerDesiredPickupTime, booking.Duration),
                                                        StartLocation = new GoogleMapPoint()
                                                        {
                                                            Latitude = startStation.Latitude,
                                                            Longitude = startStation.Longitude
                                                        },
                                                        EndLocation = new GoogleMapPoint()
                                                        {
                                                            Latitude = endStation.Latitude,
                                                            Longitude = endStation.Longitude
                                                        }
                                                    };

                    trips = trips.OrderBy(t => t.BeginTime);

                    driverTrips.Add(new DriverTripsOfDate
                    {
                        Date = tripDate,
                        Trips = trips.ToList()
                    });
                }
            }
            return driverTrips;
        }


        private async Task AddToPrioritizedBookingDetailsAsync(IList<DriverMappingItem> prioritizedBookingDetails,
            BookingDetail bookingDetailToAdd, DriverTrip addedTrip,
            DriverTrip? previousTrip, DriverTrip? nextTrip, CancellationToken cancellationToken)
        {
            //TimeSpan addedTripEndTime = addedTrip.EndTime;

            if (previousTrip is null)
            {
                if (nextTrip != null)
                {
                    // Has Next trip
                    //TimeSpan nextTripEndTime = nextTrip.EndTime;
                    // Check for duration from addedTrip EndStation to nextTrip StartStation
                    double movingDuration = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        addedTrip.EndLocation, nextTrip.StartLocation, cancellationToken);

                    if ((nextTrip.BeginTime - addedTrip.EndTime).TotalMinutes > movingDuration + 10)
                    {
                        // Valid booking details
                        prioritizedBookingDetails.Add(new DriverMappingItem(
                            bookingDetailToAdd, (nextTrip.BeginTime - addedTrip.EndTime).TotalMinutes - movingDuration));
                    }
                }
                // Previous and Next cannot be both null
            }
            else
            {
                // Has Previous trip
                //TimeSpan previousTripEndTime = previousTrip.EndTime;

                if (nextTrip is null)
                {
                    // Only previous trip

                    // Check for duration from addedTrip EndStation to nextTrip StartStation
                    double movingDuration = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        previousTrip.EndLocation, addedTrip.StartLocation, cancellationToken);

                    if ((addedTrip.BeginTime - previousTrip.EndTime).TotalMinutes > movingDuration + 10)
                    {
                        // Valid booking details
                        prioritizedBookingDetails.Add(new DriverMappingItem(
                            bookingDetailToAdd, (addedTrip.BeginTime - previousTrip.EndTime).TotalMinutes - movingDuration));
                    }
                }
                else
                {
                    // Has Previous and Next trip
                    // TimeSpan nextTripEndTime = nextTrip.EndTime;
                    // Check for duration from addedTrip EndStation to nextTrip StartStation
                    double movingDurationPrevToAdded = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        previousTrip.EndLocation, addedTrip.StartLocation, cancellationToken);
                    double movingDurationAddedToNext = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        addedTrip.EndLocation, nextTrip.StartLocation, cancellationToken);

                    double durationPrevToAdded = (addedTrip.BeginTime - previousTrip.EndTime).TotalMinutes;
                    double durationAddedToNext = (nextTrip.BeginTime - addedTrip.EndTime).TotalMinutes;

                    if (durationPrevToAdded > movingDurationPrevToAdded + 10
                        && durationAddedToNext > movingDurationAddedToNext + 10)
                    {
                        // Valid booking details
                        prioritizedBookingDetails.Add(new DriverMappingItem(
                            bookingDetailToAdd, (durationPrevToAdded - movingDurationPrevToAdded +
                                                durationAddedToNext - movingDurationAddedToNext) / 2));
                    }
                }

            }

            //TimeSpan lastTripEndTime = lastTrip.EndTime;
            //TimeSpan nextTripEndTime = DateTimeUtilities.CalculateTripEndTime(bookingDetail.CustomerDesiredPickupTime,
            //    booking.Duration);
            //if (lastTripEndTime <= bookingDetail.CustomerDesiredPickupTime)
            //{
            //    // Check for duration from lastTrip End Station to BD's Start Station
            //    int movingDuration = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
            //        lastTrip.EndLocation, new GoogleMapPoint
            //        {
            //            Latitude = startStation.Latitude,
            //            Longtitude = startStation.Longtitude
            //        }); // Minutes

            //    if ((bookingDetail.CustomerDesiredPickupTime - lastTripEndTime).TotalMinutes
            //        > movingDuration + 10)
            //    {
            //        // 10 minutes for traffic and other stuffs
            //        // Valid booking details
            //        prioritizedBookingDetails.Add(new DriverMappingItem(bookingDetail,
            //            (bookingDetail.CustomerDesiredPickupTime - lastTripEndTime).TotalMinutes - movingDuration
            //        )); // Prioritized by duration to move between stations
            //    }

            //}
        }

        private async Task CheckDriverSchedules(Guid driverId,
            BookingDetail bookingDetail,
            Booking booking,
            CancellationToken cancellationToken)
        {
            IList<DriverTripsOfDate> driverTrips = await GetDriverSchedulesAsync(driverId, cancellationToken);
            if (driverTrips.Count == 0)
            {
                // Has no trips
            }
            else
            {
                // Has trips
                DriverTripsOfDate? tripsOfDate = driverTrips.SingleOrDefault(
                    t => t.Date == DateOnly.FromDateTime(bookingDetail.Date));
                if (tripsOfDate is null || tripsOfDate.Trips.Count == 0)
                {
                    // No trips in day
                }
                else
                {
                    // Has trips in day
                    TimeSpan bookingDetailEndTime = DateTimeUtilities.CalculateTripEndTime(
                        bookingDetail.CustomerDesiredPickupTime, booking.Duration);

                    Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId,
                        cancellationToken: cancellationToken);
                    Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId,
                        cancellationToken: cancellationToken);

                    DriverTrip addedTrip = new DriverTrip
                    {
                        Id = bookingDetail.Id,
                        BeginTime = bookingDetail.CustomerDesiredPickupTime,
                        EndTime = bookingDetailEndTime,
                        StartLocation = new GoogleMapPoint
                        {
                            Latitude = startStation.Latitude,
                            Longitude = startStation.Longitude
                        },
                        EndLocation = new GoogleMapPoint
                        {
                            Latitude = endStation.Latitude,
                            Longitude = endStation.Longitude
                        }
                    };

                    IEnumerable<DriverTrip> addedTrips = tripsOfDate.Trips.Append(addedTrip)
                        .OrderBy(t => t.BeginTime);
                    LinkedList<DriverTrip> addedTripsAsLinkedList = new LinkedList<DriverTrip>(addedTrips);
                    LinkedListNode<DriverTrip> addedTripAsNode = addedTripsAsLinkedList.Find(addedTrip);

                    DriverTrip? previousTrip = addedTripAsNode.Previous?.Value;
                    DriverTrip? nextTrip = addedTripAsNode.Next?.Value;

                    if (previousTrip != null)
                    {
                        if (addedTripAsNode.Value.BeginTime <= previousTrip.EndTime)
                        {
                            // Invalid
                            throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                $"Bạn đang chọn chuyến đi có thời gian bắt đầu ({addedTripAsNode.Value.BeginTime}) " +
                                $"sớm hơn so với thời gian dự kiến bạn sẽ kết thúc một chuyến đi bạn đã chọn trước đó ({previousTrip.EndTime})");
                        }
                    }

                    if (nextTrip != null)
                    {
                        // Has Next Trip
                        if (addedTripAsNode.Value.EndTime >= nextTrip.BeginTime)
                        {
                            throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                $"Bạn đang chọn chuyến đi có thời gian kết thúc dự kiến ({addedTripAsNode.Value.EndTime}) " +
                                $"trễ hơn so với thời gian bạn phải bắt đầu một chuyến đi bạn đã chọn trước đó ({nextTrip.BeginTime})");
                        }
                    }
                }
            }
        }

        private async Task<IEnumerable<BookingDetail>> CheckDriverSchedules(Guid driverId,
            IEnumerable<BookingDetail> bookingDetails,
            Booking booking,
            CancellationToken cancellationToken)
        {
            IEnumerable<BookingDetail> errorBookingDetails = new List<BookingDetail>();

            IList<DriverTripsOfDate> driverTrips = await GetDriverSchedulesAsync(driverId, cancellationToken);
            if (driverTrips.Count == 0)
            {
                // Has no trips
            }
            else
            {
                // Has trips
                foreach (BookingDetail bookingDetail in bookingDetails)
                {
                    DriverTripsOfDate? tripsOfDate = driverTrips.SingleOrDefault(
                        t => t.Date == DateOnly.FromDateTime(bookingDetail.Date));
                    if (tripsOfDate is null || tripsOfDate.Trips.Count == 0)
                    {
                        // No trips in day
                    }
                    else
                    {
                        // Has trips in day
                        TimeSpan bookingDetailEndTime = DateTimeUtilities.CalculateTripEndTime(
                            bookingDetail.CustomerDesiredPickupTime, booking.Duration);

                        Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId,
                            cancellationToken: cancellationToken);
                        Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId,
                            cancellationToken: cancellationToken);

                        DriverTrip addedTrip = new DriverTrip
                        {
                            Id = bookingDetail.Id,
                            BeginTime = bookingDetail.CustomerDesiredPickupTime,
                            EndTime = bookingDetailEndTime,
                            StartLocation = new GoogleMapPoint
                            {
                                Latitude = startStation.Latitude,
                                Longitude = startStation.Longitude
                            },
                            EndLocation = new GoogleMapPoint
                            {
                                Latitude = endStation.Latitude,
                                Longitude = endStation.Longitude
                            }
                        };

                        IEnumerable<DriverTrip> addedTrips = tripsOfDate.Trips.Append(addedTrip)
                            .OrderBy(t => t.BeginTime);
                        LinkedList<DriverTrip> addedTripsAsLinkedList = new LinkedList<DriverTrip>(addedTrips);
                        LinkedListNode<DriverTrip> addedTripAsNode = addedTripsAsLinkedList.Find(addedTrip);

                        DriverTrip? previousTrip = addedTripAsNode.Previous?.Value;
                        DriverTrip? nextTrip = addedTripAsNode.Next?.Value;

                        if (previousTrip != null)
                        {
                            if (addedTripAsNode.Value.BeginTime <= previousTrip.EndTime)
                            {
                                // Invalid
                                //throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                //    $"Bạn đang chọn chuyến đi có thời gian bắt đầu ({addedTripAsNode.Value.BeginTime}) " +
                                //    $"sớm hơn so với thời gian dự kiến bạn sẽ kết thúc một chuyến đi bạn đã chọn trước đó ({previousTrip.EndTime})");
                                errorBookingDetails = errorBookingDetails.Append(bookingDetail);
                                tripsOfDate.Trips.Remove(addedTrip);
                            }
                        }

                        if (nextTrip != null)
                        {
                            // Has Next Trip
                            if (addedTripAsNode.Value.EndTime >= nextTrip.BeginTime)
                            {
                                //throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                //    $"Bạn đang chọn chuyến đi có thời gian kết thúc dự kiến ({addedTripAsNode.Value.EndTime}) " +
                                //    $"trễ hơn so với thời gian bạn phải bắt đầu một chuyến đi bạn đã chọn trước đó ({nextTrip.BeginTime})");
                                errorBookingDetails = errorBookingDetails.Append(bookingDetail);
                                tripsOfDate.Trips.Remove(addedTrip);
                            }
                        }
                    }
                }

            }

            return errorBookingDetails;
        }

        private async Task<IEnumerable<BookingDetail>> FilterBookingDetailsAsync(IEnumerable<BookingDetail> bookingDetails,
            BookingDetailFilterParameters filters, CancellationToken cancellationToken)
        {
            bookingDetails = bookingDetails.Where(
                d =>
                {
                    return
                    (!filters.MinDate.HasValue || DateOnly.FromDateTime(d.Date) >= filters.MinDate.Value)
                    && (!filters.MaxDate.HasValue || DateOnly.FromDateTime(d.Date) <= filters.MaxDate.Value)
                    && (!filters.MinPickupTime.HasValue || TimeOnly.FromTimeSpan(d.CustomerDesiredPickupTime) >= filters.MinPickupTime.Value)
                    && (!filters.MaxPickupTime.HasValue || TimeOnly.FromTimeSpan(d.CustomerDesiredPickupTime) >= filters.MaxPickupTime.Value);
                });

            // Filter for Status
            if (filters.Status != null && !string.IsNullOrWhiteSpace(filters.Status))
            {
                IEnumerable<BookingDetailStatus> bookingDetailStatuses = new List<BookingDetailStatus>();

                var statuses = filters.Status.Split(",");
                foreach (string status in statuses)
                {
                    if (Enum.TryParse(typeof(BookingDetailStatus), status.Trim(),
                        true, out object? result))
                    {
                        if (result != null)
                        {
                            bookingDetailStatuses = bookingDetailStatuses.Append((BookingDetailStatus)result);
                        }
                    }
                }

                if (bookingDetailStatuses.Any())
                {
                    bookingDetails = bookingDetails.Where(b => bookingDetailStatuses.Contains(b.Status));
                }
            }

            // Filter for Type
            if (filters.Type != null && !string.IsNullOrWhiteSpace(filters.Type))
            {
                IEnumerable<BookingDetailType> bookingDetailTypes = new List<BookingDetailType>();

                var types = filters.Type.Split(",");
                foreach (string type in types)
                {
                    if (Enum.TryParse(typeof(BookingDetailType), type.Trim(),
                        true, out object? result))
                    {
                        if (result != null)
                        {
                            bookingDetailTypes = bookingDetailTypes.Append((BookingDetailType)result);
                        }
                    }
                }

                if (bookingDetailTypes.Any())
                {
                    bookingDetails = bookingDetails.Where(b => bookingDetailTypes.Contains(b.Type));
                }
            }

            IEnumerable<Guid> removedBookingDetailIds = new List<Guid>();

            foreach (BookingDetail bookingDetail in bookingDetails)
            {
                if (filters.StartLocationLat.HasValue && filters.StartLocationLng.HasValue
                && filters.StartLocationRadius.HasValue)
                {
                    Station startStation = await work.Stations.GetAsync(
                        bookingDetail.StartStationId, cancellationToken: cancellationToken);
                    GoogleMapPoint startPoint = new GoogleMapPoint(startStation.Latitude, startStation.Longitude);
                    GoogleMapPoint conditionPoint = new GoogleMapPoint(filters.StartLocationLat.Value,
                        filters.StartLocationLng.Value);

                    double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        conditionPoint, startPoint, cancellationToken);

                    if (distance > filters.StartLocationRadius.Value)
                    {
                        // Outside of radius
                        removedBookingDetailIds = removedBookingDetailIds.Append(bookingDetail.Id);
                    }
                }

                if (filters.EndLocationLat.HasValue && filters.EndLocationLng.HasValue
                    && filters.EndLocationRadius.HasValue)
                {
                    Station endStation = await work.Stations.GetAsync(
                        bookingDetail.EndStationId, cancellationToken: cancellationToken);
                    GoogleMapPoint startPoint = new GoogleMapPoint(endStation.Latitude, endStation.Longitude);
                    GoogleMapPoint conditionPoint = new GoogleMapPoint(filters.EndLocationLat.Value,
                        filters.EndLocationLng.Value);

                    double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        conditionPoint, startPoint, cancellationToken);

                    if (distance > filters.EndLocationRadius.Value)
                    {
                        // Outside of radius
                        removedBookingDetailIds = removedBookingDetailIds.Append(bookingDetail.Id);
                    }
                }
            }

            removedBookingDetailIds = removedBookingDetailIds.Distinct();
            bookingDetails = bookingDetails.Where(d => !removedBookingDetailIds.Contains(d.Id));

            return bookingDetails;
        }

        private async Task<IEnumerable<DriverMappingItem>> FilterBookingDetailsAsync(IEnumerable<DriverMappingItem> bookingDetails,
            BookingDetailFilterParameters filters, CancellationToken cancellationToken)
        {
            bookingDetails = bookingDetails.Where(
                d =>
                {
                    return
                    (!filters.MinDate.HasValue || DateOnly.FromDateTime(d.BookingDetail.Date) >= filters.MinDate.Value)
                    && (!filters.MaxDate.HasValue || DateOnly.FromDateTime(d.BookingDetail.Date) <= filters.MaxDate.Value)
                    && (!filters.MinPickupTime.HasValue || TimeOnly.FromTimeSpan(d.BookingDetail.CustomerDesiredPickupTime) >= filters.MinPickupTime.Value)
                    && (!filters.MaxPickupTime.HasValue || TimeOnly.FromTimeSpan(d.BookingDetail.CustomerDesiredPickupTime) >= filters.MaxPickupTime.Value);
                });

            // Filter for Status
            if (filters.Status != null && !string.IsNullOrWhiteSpace(filters.Status))
            {
                IEnumerable<BookingDetailStatus> bookingDetailStatuses = new List<BookingDetailStatus>();

                var statuses = filters.Status.Split(",");
                foreach (string status in statuses)
                {
                    if (Enum.TryParse(typeof(BookingDetailStatus), status.Trim(),
                        true, out object? result))
                    {
                        if (result != null)
                        {
                            bookingDetailStatuses = bookingDetailStatuses.Append((BookingDetailStatus)result);
                        }
                    }
                }

                if (bookingDetailStatuses.Any())
                {
                    bookingDetails = bookingDetails.Where(b =>
                    bookingDetailStatuses.Contains(b.BookingDetail.Status));
                }
            }

            // Station filter

            IEnumerable<Guid> removedBookingDetailIds = new List<Guid>();
            IEnumerable<Guid> removedStartStationIds = new List<Guid>();
            IDictionary<Guid, double> startStationDistances = new Dictionary<Guid, double>();

            if (filters.StartLocationLat.HasValue && filters.StartLocationLng.HasValue
               && filters.StartLocationRadius.HasValue)
            {
                IEnumerable<Guid> startStationIds = bookingDetails.Select(d => d.BookingDetail.StartStationId).Distinct();
                IEnumerable<Station> startStations = await work.Stations.GetAllAsync(
                       query => query.Where(s => startStationIds.Contains(s.Id)),
                       cancellationToken: cancellationToken);

                foreach (Station startStation in startStations)
                {
                    GoogleMapPoint startPoint = new GoogleMapPoint(startStation.Latitude, startStation.Longitude);
                    GoogleMapPoint conditionPoint = new GoogleMapPoint(filters.StartLocationLat.Value,
                        filters.StartLocationLng.Value);

                    double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        conditionPoint, startPoint, cancellationToken);

                    if (distance > filters.StartLocationRadius.Value)
                    {
                        // Outside of radius
                        //removedBookingDetailIds = removedBookingDetailIds.Append(bookingDetail.Id);
                        removedStartStationIds = removedStartStationIds.Append(startStation.Id);
                    }
                    else
                    {
                        //item.PrioritizedPoint += distance;
                        startStationDistances.Add(startStation.Id, distance);
                    }
                }
            }

            IEnumerable<Guid> removedEndStationIds = new List<Guid>();
            IDictionary<Guid, double> endStationDistances = new Dictionary<Guid, double>();

            if (filters.EndLocationLat.HasValue && filters.EndLocationLng.HasValue
                    && filters.EndLocationRadius.HasValue)
            {
                IEnumerable<Guid> endStationIds = bookingDetails.Select(d => d.BookingDetail.EndStationId).Distinct();
                IEnumerable<Station> endStations = await work.Stations.GetAllAsync(
                    query => query.Where(s => endStationIds.Contains(s.Id)),
                    cancellationToken: cancellationToken);

                foreach (Station endStation in endStations)
                {

                    GoogleMapPoint startPoint = new GoogleMapPoint(endStation.Latitude, endStation.Longitude);
                    GoogleMapPoint conditionPoint = new GoogleMapPoint(filters.EndLocationLat.Value,
                        filters.EndLocationLng.Value);

                    double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        conditionPoint, startPoint, cancellationToken);

                    if (distance > filters.EndLocationRadius.Value)
                    {
                        // Outside of radius
                        //removedBookingDetailIds = removedBookingDetailIds.Append(bookingDetail.Id);
                        removedEndStationIds = removedEndStationIds.Append(endStation.Id);
                    }
                    else
                    {
                        //item.PrioritizedPoint += distance;
                        endStationDistances.Add(endStation.Id, distance);
                    }
                }
            }

            //    foreach (DriverMappingItem item in bookingDetails)
            //{
            //    BookingDetail bookingDetail = item.BookingDetail;

            //    if (filters.StartLocationLat.HasValue && filters.StartLocationLng.HasValue
            //    && filters.StartLocationRadius.HasValue)
            //    {
            //        Station startStation = await work.Stations.GetAsync(
            //            bookingDetail.StartStationId, includeDeleted: true, cancellationToken: cancellationToken);
            //        GoogleMapPoint startPoint = new GoogleMapPoint(startStation.Latitude, startStation.Longitude);
            //        GoogleMapPoint conditionPoint = new GoogleMapPoint(filters.StartLocationLat.Value,
            //            filters.StartLocationLng.Value);

            //        double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
            //            conditionPoint, startPoint, cancellationToken);

            //        if (distance > filters.StartLocationRadius.Value)
            //        {
            //            // Outside of radius
            //            removedBookingDetailIds = removedBookingDetailIds.Append(bookingDetail.Id);
            //        }
            //        else
            //        {
            //            item.PrioritizedPoint += distance;
            //        }
            //    }

            //    if (filters.EndLocationLat.HasValue && filters.EndLocationLng.HasValue
            //        && filters.EndLocationRadius.HasValue)
            //    {
            //        Station endStation = await work.Stations.GetAsync(
            //            bookingDetail.EndStationId, includeDeleted: true, cancellationToken: cancellationToken);
            //        GoogleMapPoint startPoint = new GoogleMapPoint(endStation.Latitude, endStation.Longitude);
            //        GoogleMapPoint conditionPoint = new GoogleMapPoint(filters.EndLocationLat.Value,
            //            filters.EndLocationLng.Value);

            //        double distance = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
            //            conditionPoint, startPoint, cancellationToken);

            //        if (distance > filters.EndLocationRadius.Value)
            //        {
            //            // Outside of radius
            //            removedBookingDetailIds = removedBookingDetailIds.Append(bookingDetail.Id);
            //        }
            //        else
            //        {
            //            item.PrioritizedPoint += distance;
            //        }
            //    }
            //}

            //removedBookingDetailIds = removedBookingDetailIds.Distinct();
            removedStartStationIds = removedStartStationIds.Distinct();
            removedEndStationIds = removedEndStationIds.Distinct();

            //bookingDetails = bookingDetails.Where(d => !removedBookingDetailIds.Contains(d.BookingDetail.Id));
            bookingDetails = bookingDetails.Where(d =>
                !removedStartStationIds.Contains(d.BookingDetail.StartStationId) &&
                !removedEndStationIds.Contains(d.BookingDetail.EndStationId));
            foreach (DriverMappingItem item in bookingDetails)
            {
                if (startStationDistances.TryGetValue(item.BookingDetail.StartStationId,
                    out double startStationDistance))
                {
                    item.PrioritizedPoint += startStationDistance;
                }
                if (endStationDistances.TryGetValue(item.BookingDetail.EndStationId,
                    out double endStationDistance))
                {
                    item.PrioritizedPoint += endStationDistance;
                }
            }

            bookingDetails = bookingDetails.OrderBy(d => d.PrioritizedPoint);

            return bookingDetails;
        }

        private async Task<BookingDetailPrice> GetBookingDetailPriceAsync(
            Guid bookingId, CancellationToken cancellationToken)
        {
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    d => d.BookingId.Equals(bookingId)),
                    cancellationToken: cancellationToken);

            IEnumerable<double> prices = bookingDetails.Select(d => d.Price.Value).Distinct()
                .OrderBy(d => d);
            if (prices.Any())
            {
                if (prices.Count() == 1)
                {
                    // Only one price

                    // Maybe one price for base, one price for additional fare

                    Booking booking = await work.Bookings.GetAsync(bookingId, cancellationToken: cancellationToken);

                    //FareServices fareServices = new FareServices(work, _logger);

                    IEnumerable<TimeSpan> pickupTimes = bookingDetails.Select(d => d.CustomerDesiredPickupTime)
                        .Distinct();

                    // Only one time
                    TimeSpan endTimeSpan = DateTimeUtilities.CalculateTripEndTime(pickupTimes.First(), booking.Duration);
                    TimeOnly endTime = TimeOnly.FromTimeSpan(endTimeSpan);
                    if (FareServices.IsNightTrip(TimeOnly.FromTimeSpan(pickupTimes.First()), endTime))
                    {
                        // Night Trip
                        VehicleType? vehicleType = await work.VehicleTypes.GetAsync(booking.VehicleTypeId,
                            cancellationToken: cancellationToken);
                        if (vehicleType is null)
                        {
                            throw new ApplicationException("Thông tin phương tiện di chuyển không hợp lệ!!");
                        }
                        Setting? settingNightTrip = null;
                        switch (vehicleType.Type)
                        {
                            case VehicleSubType.VI_RIDE:
                                settingNightTrip = await work.Settings.GetAsync(
                                    s => s.Key.Equals(SettingKeys.NightTripExtraFeeBike_Key),
                                    cancellationToken: cancellationToken);
                                break;

                        }

                        if (settingNightTrip != null)
                        {
                            return new BookingDetailPrice
                            {
                                BasePrice = prices.First() - double.Parse(settingNightTrip.Value),
                                PriceWithAdditionalFare = prices.First()
                            };

                        }
                        else
                        {
                            return new BookingDetailPrice
                            {
                                BasePrice = prices.First(),
                                PriceWithAdditionalFare = prices.First()
                            };
                        }
                    }
                    else
                    {
                        // Not night trip
                        return new BookingDetailPrice
                        {
                            BasePrice = prices.First(),
                            PriceWithAdditionalFare = prices.First()
                        };
                    }
                }
                else if (prices.Count() == 2)
                {
                    return new BookingDetailPrice
                    {
                        BasePrice = prices.First(),
                        PriceWithAdditionalFare = prices.Last()
                    };
                }
                else
                {
                    return new BookingDetailPrice
                    {
                        BasePrice = prices.First(),
                        PriceWithAdditionalFare = 0
                    };
                }
            }

            return new BookingDetailPrice
            {
                BasePrice = 0,
                PriceWithAdditionalFare = 0
            };
        }
        #endregion
    }
}
