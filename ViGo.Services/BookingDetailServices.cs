using Google.Apis.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.GoogleMaps;
using ViGo.Models.Notifications;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Routes;
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.BackgroundTasks;
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
            GetDriverAssignedBookingDetailsAsync(Guid driverId,
            PaginationParameter pagination,
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

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                    bd.DriverId.Value.Equals(driverId)), cancellationToken: cancellationToken);

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
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    d => d.BookingId.Equals(bookingId)), cancellationToken: cancellationToken);

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

        public async Task<BookingDetail> UpdateBookingDetailStatusAsync(
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
                || updateDto.Status == BookingDetailStatus.GOING
                || updateDto.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
            {
                if (!updateDto.Time.HasValue)
                {
                    throw new ApplicationException("Dữ liệu truyền đi bị thiếu thời gian cập nhật!!");
                }

                // TODO Code
                // Time validation
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

            bookingDetail.Status = updateDto.Status;

            Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId,
                cancellationToken: cancellationToken);
            
            string title = "";
            string description = "";

            switch (updateDto.Status)
            {
                case BookingDetailStatus.ARRIVE_AT_PICKUP:
                    bookingDetail.ArriveAtPickupTime = updateDto.Time;

                    Station startStation = await work.Stations.GetAsync(
                        bookingDetail.StartStationId, cancellationToken: cancellationToken);

                    title = "Tài xế đã đến điểm đón - " + startStation.Name;
                    description = "Hãy chắc chắn rằng bạn lên đúng xe nhé!";
                    break;
                case BookingDetailStatus.GOING:
                    bookingDetail.PickupTime = updateDto.Time;

                    title = "Chuyến đi của bạn đã được bắt đầu!";
                    description = "Chúc bạn có một chuyến đi an toàn và vui vẻ nhé!";
                    break;
                case BookingDetailStatus.ARRIVE_AT_DROPOFF:
                    bookingDetail.DropoffTime = updateDto.Time;

                    Station startStationDropOff = await work.Stations.GetAsync(
                        bookingDetail.StartStationId, cancellationToken: cancellationToken);

                    Station endStation = await work.Stations.GetAsync(
                        bookingDetail.EndStationId, cancellationToken: cancellationToken);
                    title = "Chuyến đi của bạn đã hoàn thành!";
                    description = $"{bookingDetail.PickUpDateTime()}, từ " +
                                $"{startStationDropOff.Name} đến {endStation.Name}";
                    break;
            }

            //NotificationCreateModel notification = new NotificationCreateModel()
            //{
            //    Type = NotificationType.SPECIFIC_USER,
            //    UserId = booking.CustomerId,
            //    Title = title,
            //    Description = description
            //};

            await work.BookingDetails.UpdateAsync(bookingDetail);

            // Send notification to Customer
            User customer = await work.Users.GetAsync(booking.CustomerId,
                cancellationToken: cancellationToken);

            string? customerFcm = customer.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.BookingDetail },
                { "bookingDetailId", bookingDetail.Id.ToString() },
            };

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
            }

            if (updateDto.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
            {
                // Calculate driver wage
                FareServices fareServices = new FareServices(work, _logger);

                if (!bookingDetail.PriceAfterDiscount.HasValue
                    || !bookingDetail.Price.HasValue)
                {
                    throw new ApplicationException("Chuyến đi thiếu thông tin dữ liệu!!");
                }

                double driverWage = await fareServices.CalculateDriverWage(
                    bookingDetail.Price.Value, cancellationToken);

                // Get Customer Wallet
                Wallet customerWallet = await work.Wallets.GetAsync(
                    w => w.UserId.Equals(customer.Id), cancellationToken: cancellationToken);

                WalletTransaction customerTransaction_Withdrawal = new WalletTransaction
                {
                    WalletId = customerWallet.Id,
                    Amount = bookingDetail.PriceAfterDiscount.Value,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_PAID,
                    Status = WalletTransactionStatus.PENDING
                };
                if (customerWallet.Balance >= bookingDetail.PriceAfterDiscount.Value)
                {
                    customerTransaction_Withdrawal.Status = WalletTransactionStatus.SUCCESSFULL;
                    
                    customerWallet.Balance -= bookingDetail.PriceAfterDiscount.Value;
                        }

                // Get SYSTEM WALLET
                Wallet systemWallet = await work.Wallets.GetAsync(w =>
                    w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);
                if (systemWallet is null)
                {
                    throw new Exception("Chưa có ví dành cho hệ thống!!");
                }

                WalletTransaction systemTransaction_Add = new WalletTransaction
                {
                    WalletId = systemWallet.Id,
                    Amount = driverWage,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_PAID,
                    Status = WalletTransactionStatus.SUCCESSFULL,
                };

                Wallet driverWallet = await work.Wallets.GetAsync(w =>
                    w.UserId.Equals(bookingDetail.DriverId.Value), cancellationToken: cancellationToken);
                if (driverWallet is null)
                {
                    throw new ApplicationException("Tài xế chưa được cấu hình ví!!");
                }

                WalletTransaction driverTransaction_Add = new WalletTransaction
                {
                    WalletId = driverWallet.Id,
                    Amount = driverWage,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_INCOME,
                    Status = WalletTransactionStatus.SUCCESSFULL
                };

                systemWallet.Balance -= driverWage;
                driverWallet.Balance += driverWage;

                await work.WalletTransactions.InsertAsync(customerTransaction_Withdrawal, cancellationToken: cancellationToken);
                await work.WalletTransactions.InsertAsync(systemTransaction_Add, cancellationToken: cancellationToken);
                await work.WalletTransactions.InsertAsync(driverTransaction_Add, cancellationToken: cancellationToken);

                await work.Wallets.UpdateAsync(customerWallet);
                await work.Wallets.UpdateAsync(systemWallet);
                await work.Wallets.UpdateAsync(driverWallet);
            }

            await work.SaveChangesAsync(cancellationToken);

            if (updateDto.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
            {
                // Send notification to request rating
                if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
                {
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

                // Trigger Background Tasks for calculating Driver Wage
            }

            return bookingDetail;
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
                    Description = $"{bookingDetail.PickUpDateTime()}, từ " +
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
                    Description = $"{bookingDetail.PickUpDateTime()}, từ " +
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
            double driverWage = await fareServices.CalculateDriverWage(bookingDetail.PriceAfterDiscount.Value, cancellationToken);
            return driverWage;
        }

        public async Task<IPagedEnumerable<BookingDetailViewModel>>
            GetDriverAvailableBookingDetailsAsync(Guid driverId,
                PaginationParameter pagination, HttpContext context,
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
                    bd => !bd.DriverId.HasValue
                    && bd.Status == BookingDetailStatus.PENDING_ASSIGN
                    // Only for future ones
                    && bd.Date >= DateTimeUtilities.GetDateTimeVnNow()),
                    cancellationToken: cancellationToken);

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

            int totalCount = prioritizedBookingDetails.Count;
            prioritizedBookingDetails = prioritizedBookingDetails.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data.ToList();

            IEnumerable<Guid> customerRoutineIds = prioritizedBookingDetails.Select(
                p => p.BookingDetail.CustomerRouteRoutineId);
            IEnumerable<RouteRoutine> customerRoutines = await work.RouteRoutines.GetAllAsync(
                query => query.Where(r => customerRoutineIds.Contains(r.Id)), cancellationToken: cancellationToken);

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

            bookingDetail.DriverId = driverId;
            bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
            bookingDetail.Status = BookingDetailStatus.ASSIGNED;

            await work.BookingDetails.UpdateAsync(bookingDetail);
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
                    Description = $"{bookingDetail.PickUpDateTime()}, từ " +
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
                    Description = $"{bookingDetail.PickUpDateTime()}, từ " +
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

        public async Task<(BookingDetail, Guid?, bool)> CancelBookingDetailAsync(Guid bookingDetailId,
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

            if (bookingDetail.Status == BookingDetailStatus.CANCELLED)
            {
                return (bookingDetail, null, false);
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
            }

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
                else if (difference.TotalHours >= 1)
                {
                    // >= 1 hour but < 6h
                    // Been selected
                    // 10% extra fee
                    chargeFee = 0.1;
                }
                else
                {
                    // 100% extra fee
                    // No fee
                    chargeFee = 1;
                }
            }

            double chargeFeeAmount = bookingDetail.PriceAfterDiscount.Value * chargeFee;
            chargeFeeAmount = FareUtilities.RoundToThousands(chargeFeeAmount);

            if (cancelledUser != null && chargeFeeAmount > 0)
            {
                // User is driver or customer
                Wallet wallet = await work.Wallets.GetAsync(
                    w => w.UserId.Equals(cancelledUser.Id),
                    cancellationToken: cancellationToken);

                WalletTransaction walletTransaction = new WalletTransaction
                {
                    WalletId = wallet.Id,
                    BookingDetailId = bookingDetail.Id,
                    Amount = chargeFeeAmount,
                    Type = WalletTransactionType.CANCEL_FEE,
                    Status = WalletTransactionStatus.PENDING
                };

                await work.WalletTransactions.InsertAsync(walletTransaction,
                    cancellationToken: cancellationToken);

                if (wallet.Balance >= chargeFeeAmount)
                {
                    // Able to be substracted the amount
                    wallet.Balance -= chargeFeeAmount;

                    walletTransaction.Status = WalletTransactionStatus.SUCCESSFULL;

                    Wallet systemWallet = await work.Wallets.GetAsync(
                        w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);
                    WalletTransaction systemTransaction = new WalletTransaction
                    {
                        WalletId = systemWallet.Id,
                        BookingDetailId = bookingDetail.Id,
                        Amount = chargeFeeAmount,
                        Type = WalletTransactionType.CANCEL_FEE,
                        Status = WalletTransactionStatus.SUCCESSFULL
                    };

                    systemWallet.Balance += chargeFeeAmount;

                    await work.WalletTransactions.InsertAsync(systemTransaction,
                    cancellationToken: cancellationToken);
                    await work.Wallets.UpdateAsync(systemWallet);

                    await work.Wallets.UpdateAsync(wallet);
                }

            }

            bookingDetail.Status = BookingDetailStatus.CANCELLED;

            bookingDetail.CanceledUserId = IdentityUtilities.GetCurrentUserId();

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

            User customer = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);

            string? driverFcm = driver?.FcmToken;
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

            if (driver != null 
                && driverFcm != null && !string.IsNullOrEmpty(driverFcm))
            {
                NotificationCreateModel driverNotification = new NotificationCreateModel()
                {
                    UserId = driver.Id,
                    Title = "Chuyến đi đã bị hủy!",
                    Description = $"{bookingDetail.PickUpDateTime()}, từ " +
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
                    Description = $"{bookingDetail.PickUpDateTime()}, từ " +
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

            return (bookingDetail, cancelledUser?.Id, isInWeek);
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

            if(booking.CustomerId != IdentityUtilities.GetCurrentUserId())
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
                    Description = $"",
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

        #region Private
        private async Task<IList<DriverTripsOfDate>> GetDriverSchedulesAsync(Guid driverId,
            CancellationToken cancellationToken)
        {
            // Get driver current schedules
            IList<DriverTripsOfDate> driverTrips = new List<DriverTripsOfDate>();
            IEnumerable<BookingDetail> driverBookingDetails = await
                work.BookingDetails.GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                        bd.DriverId.Value.Equals(driverId)), cancellationToken: cancellationToken);

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
                    IEnumerable<DriverTrip> trips = from detail in driverBookingDetails
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
                    int movingDuration = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
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
                    int movingDuration = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
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
                    //TimeSpan nextTripEndTime = nextTrip.EndTime;
                    // Check for duration from addedTrip EndStation to nextTrip StartStation
                    int movingDurationPrevToAdded = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        previousTrip.EndLocation, addedTrip.StartLocation, cancellationToken);
                    int movingDurationAddedToNext = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
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

                    IEnumerable<DriverTrip> addedTrips = tripsOfDate.Trips.Append(addedTrip);
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

        #endregion
    }
}
