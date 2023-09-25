using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Data;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.Bookings;
using ViGo.Models.GoogleMaps;
using ViGo.Models.Notifications;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Routes;
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Google;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class BookingServices : UseNotificationServices
    {
        public BookingServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<BookingViewModel>>
            GetBookingsAsync(Guid? userId,
            PaginationParameter pagination, BookingSortingParameters sorting,
            BookingFilterParameters filters,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                userId = IdentityUtilities.GetCurrentUserId();
            }

            User? user = null;
            if (userId.HasValue)
            {
                user = await work.Users.GetAsync(userId.Value, cancellationToken: cancellationToken);
                if (user is null)
                {
                    throw new ApplicationException("Người dùng không tồn tại!!!");
                }
                if (user.Role != UserRole.CUSTOMER && user.Role != UserRole.DRIVER)
                {
                    throw new ApplicationException("Vai trò người dùng không hợp lệ!!");
                }
            }

            IEnumerable<Booking> bookings = new List<Booking>();

            if (userId is null)
            {
                // Get All the Bookings
                bookings = await work.Bookings.GetAllAsync(cancellationToken: cancellationToken);

            }
            else if (user.Role == UserRole.CUSTOMER)
            {
                bookings = await work.Bookings.GetAllAsync(
                    query => query.Where(b => b.CustomerId.Equals(user.Id)),
                    cancellationToken: cancellationToken);

            }
            else
            {
                // Driver
                IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                    .GetAllAsync(query => query.Where(d => d.DriverId.HasValue &&
                    d.DriverId.Value.Equals(user.Id)), cancellationToken: cancellationToken);
                IEnumerable<Guid> bookingIds = bookingDetails.Select(d => d.BookingId);
                bookings = await work.Bookings.GetAllAsync(query => query.Where(
                    b => bookingIds.Contains(b.Id)), cancellationToken: cancellationToken);
            }
            //IEnumerable<Booking> bookings =
            //    await work.Bookings.GetAllAsync(
            //        query => query.Where(
            //            b =>
            //            (userId != null && userId.HasValue) ?
            //            b.CustomerId.Equals(userId.Value)
            //            : true), cancellationToken: cancellationToken);

            bookings = await FilterBookings(bookings, filters, cancellationToken);

            bookings = bookings.Sort(sorting.OrderBy);

            int totalRecords = bookings.Count();

            bookings = bookings.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> customerIds = bookings.Select(b => b.CustomerId).Distinct();
            //IEnumerable<Guid> routeStationIds = bookings.Select(
            //    b => b.StartRouteStationId).Concat(bookings.Select(
            //        b => b.EndRouteStationId))
            //    .Distinct();

            IEnumerable<User> users = await work.Users
                .GetAllAsync(query => query.Where(
                    u => customerIds.Contains(u.Id)),
                    cancellationToken: cancellationToken);
            IEnumerable<UserViewModel> userDtos =
                from userDto in users
                select new UserViewModel(userDto);

            IEnumerable<Guid> customerRouteIds = bookings.Select(b => b.CustomerRouteId).Distinct();
            IEnumerable<Route> customerRoutes = await work.Routes.GetAllAsync(
                query => query.Where(
                    r => customerRouteIds.Contains(r.Id)), cancellationToken: cancellationToken);

            ////IEnumerable<RouteStation> routeStations = await work.RouteStations
            ////    .GetAllAsync(query => query.Where(
            ////        rs => routeStationIds.Contains(rs.Id)), cancellationToken: cancellationToken);
            IEnumerable<Guid> stationIds =
                customerRoutes.Select(rs => rs.StartStationId)
                .Concat(customerRoutes.Select(r => r.EndStationId))
                .Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> vehicleTypeIds = bookings.Select(b => b.VehicleTypeId).Distinct();
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes
                .GetAllAsync(query => query.Where(
                    v => vehicleTypeIds.Contains(v.Id)), cancellationToken: cancellationToken);

            IEnumerable<BookingViewModel> dtos =
                from booking in bookings
                join customer in userDtos
                    on booking.CustomerId equals customer.Id
                join route in customerRoutes
                    on booking.CustomerRouteId equals route.Id
                //join startRouteStation in routeStations
                //    on booking.StartRouteStationId equals startRouteStation.Id
                join startStation in stations
                    on route.StartStationId equals startStation.Id
                //join endRouteStation in routeStations
                //    on booking.EndRouteStationId equals endRouteStation.Id
                join endStation in stations
                    on route.EndStationId equals endStation.Id
                join vehicleType in vehicleTypes
                    on booking.VehicleTypeId equals vehicleType.Id
                select new BookingViewModel(
                    booking, customer, route,
                    new StationViewModel(startStation),
                    new StationViewModel(endStation),
                    vehicleType
                    );

            dtos = dtos.ToList();

            foreach (BookingViewModel booking in dtos)
            {
                (int total, int assigned, int completed) = await CountBookingDetailsAsync(booking.Id, cancellationToken);
                booking.TotalBookingDetailsCount = total;
                booking.TotalAssignedBookingDetailsCount = assigned;
                booking.TotalCompletedBookingDetailsCount = completed;
            }
            //IEnumerable<BookingViewModel> dtos
            //    = from booking in bookings
            //      select new BookingViewModel(booking);

            return dtos.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<IPagedEnumerable<BookingViewModel>>
            GetAvailableBookingsAsync(Guid driverId,
            PaginationParameter pagination, BookingSortingParameters sorting,
            BookingFilterParameters filters, BookingDetailFilterParameters? bookingDetailFilters,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin())
            {
                driverId = IdentityUtilities.GetCurrentUserId();
            }

            User driver = await work.Users.GetAsync(driverId, cancellationToken: cancellationToken);
            if (driver is null || driver.Role != UserRole.DRIVER)
            {
                throw new ApplicationException("Tài xế không tồn tại!!!");
            }

            // Get Available Booking Details
            BookingDetailServices bookingDetailServices = new BookingDetailServices(work, _logger);
            IPagedEnumerable<BookingDetailViewModel> availableBookingDetailModels
                = await bookingDetailServices.GetDriverAvailableBookingDetailsAsync(driverId,
                null, new PaginationParameter(1, -1), bookingDetailFilters,
                context, cancellationToken);

            IEnumerable<BookingDetailViewModel> availableBookingDetails = availableBookingDetailModels.Data;
            IEnumerable<Guid> bookingIds = availableBookingDetails.Select(d => d.BookingId)
                .Distinct();

            IEnumerable<Booking> bookings = await work.Bookings
                .GetAllAsync(query => query.Where(
                    b => bookingIds.Contains(b.Id)), cancellationToken: cancellationToken);

            bookings = await FilterBookings(bookings, filters, cancellationToken);

            bookings = bookings.Sort(sorting.OrderBy);

            int totalRecords = bookings.Count();

            bookings = bookings.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> customerIds = bookings.Select(b => b.CustomerId).Distinct();

            IEnumerable<User> users = await work.Users
                .GetAllAsync(query => query.Where(
                    u => customerIds.Contains(u.Id)),
                    cancellationToken: cancellationToken);

            IEnumerable<UserViewModel> userDtos =
                from user in users
                select new UserViewModel(user);

            IEnumerable<Guid> customerRouteIds = bookings.Select(b => b.CustomerRouteId).Distinct();
            IEnumerable<Route> customerRoutes = await work.Routes.GetAllAsync(
                query => query.Where(
                    r => customerRouteIds.Contains(r.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> stationIds =
                customerRoutes.Select(rs => rs.StartStationId)
                .Concat(customerRoutes.Select(r => r.EndStationId))
                .Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> vehicleTypeIds = bookings.Select(b => b.VehicleTypeId).Distinct();
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes
                .GetAllAsync(query => query.Where(
                    v => vehicleTypeIds.Contains(v.Id)), cancellationToken: cancellationToken);

            IEnumerable<BookingViewModel> dtos =
                from booking in bookings
                join customer in userDtos
                    on booking.CustomerId equals customer.Id
                join route in customerRoutes
                    on booking.CustomerRouteId equals route.Id
                join startStation in stations
                    on route.StartStationId equals startStation.Id
                join endStation in stations
                    on route.EndStationId equals endStation.Id
                join vehicleType in vehicleTypes
                    on booking.VehicleTypeId equals vehicleType.Id
                select new BookingViewModel(
                    booking, customer, route,
                    new StationViewModel(startStation),
                    new StationViewModel(endStation),
                    vehicleType
                    );

            dtos = dtos.ToList();

            foreach (BookingViewModel booking in dtos)
            {
                (int total, int assigned, int completed) = await CountBookingDetailsAsync(booking.Id, cancellationToken);
                booking.TotalBookingDetailsCount = total;
                booking.TotalAssignedBookingDetailsCount = assigned;
                booking.TotalCompletedBookingDetailsCount = completed;
            }

            return dtos.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<BookingViewModel?> GetBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        {
            Booking booking = await work.Bookings.GetAsync(bookingId, cancellationToken: cancellationToken);
            if (booking == null)
            {
                return null;
            }

            User customer = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);
            UserViewModel customerDto = new UserViewModel(customer);

            //IEnumerable<RouteStation> routeStations = await work.RouteStations
            //    .GetAllAsync(query => query.Where(
            //        rs => rs.Id.Equals(booking.StartRouteStationId)
            //        || rs.Id.Equals(booking.EndRouteStationId)), cancellationToken: cancellationToken);

            //IEnumerable<Guid> stationIds = booking.Select(rs => rs.StationId).Distinct();
            //IEnumerable<Station> stations = await work.Stations
            //    .GetAllAsync(query => query.Where(
            //        s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

            //RouteStation startStation = routeStations.SingleOrDefault(rs => rs.Id.Equals(booking.StartRouteStationId));
            //RouteStationViewModel startStationDto = new RouteStationViewModel(
            //    startStation, stations.SingleOrDefault(s => s.Id.Equals(startStation.StationId))
            //    );

            //RouteStation endStation = routeStations.SingleOrDefault(rs => rs.Id.Equals(booking.EndRouteStationId));
            //RouteStationViewModel endStationDto = new RouteStationViewModel(
            //    endStation, stations.SingleOrDefault(s => s.Id.Equals(endStation.StationId)));
            Route route = await work.Routes.GetAsync(booking.CustomerRouteId,
                cancellationToken: cancellationToken);
            Station startStation = await work.Stations.GetAsync(route.StartStationId,
                    includeDeleted: true, cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(route.EndStationId,
                    includeDeleted: true, cancellationToken: cancellationToken);

            VehicleType vehicleType = await work.VehicleTypes.GetAsync(booking.VehicleTypeId, cancellationToken: cancellationToken);

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.BookingId.Equals(booking.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid?> driverIds = bookingDetails.Select(bd => bd.DriverId);
            driverIds = driverIds.Where(d => d.HasValue)
                .Distinct();
            IEnumerable<User> drivers = await work.Users.GetAllAsync(query => query.Where(
                    u => driverIds.Contains(u.Id)), cancellationToken: cancellationToken);

            //IList<BookingDetailViewModel> bookingDetailDtos = new List<BookingDetailViewModel>();
            //foreach (BookingDetail bookingDetail in bookingDetails)
            //{
            //    UserViewModel? driver = null;
            //    if (bookingDetail.DriverId.HasValue)
            //    {
            //        driver = new UserViewModel(
            //            drivers.SingleOrDefault(
            //            d => d.Id.Equals(bookingDetail.DriverId.Value)));
            //    }
            //    bookingDetailDtos.Add(new BookingDetailViewModel(bookingDetail, driver));
            //}

            (int total, int assigned, int completed) = await CountBookingDetailsAsync(booking.Id, cancellationToken);

            BookingViewModel dto = new BookingViewModel(booking,
                customerDto, route,
                new StationViewModel(startStation),
                new StationViewModel(endStation), vehicleType, total, assigned, completed);
            //BookingViewModel dto = new BookingViewModel(booking);

            return dto;
        }

        public async Task<Booking> CreateBookingAsync(
            BookingCreateModel model, CancellationToken cancellationToken)
        {
            //if (!Enum.IsDefined(model.PaymentMethod))
            //{
            //    throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
            //}
            //if (!Enum.IsDefined(model.Type))
            //{
            //    throw new ApplicationException("Loại Booking không hợp lệ!!");
            //}
            if (model.Distance <= 0)
            {
                throw new ApplicationException("Khoảng cách phải lớn hơn 0!");
            }
            if (model.Duration <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển phải lớn hơn 0!");
            }

            if (!IdentityUtilities.IsAdmin())
            {
                model.CustomerId = IdentityUtilities.GetCurrentUserId();
            }
            else
            {
                if (model.CustomerId is null)
                {
                    throw new ApplicationException("Thông tin khách hàng không hợp lệ!!");
                }
            }

            User user = await work.Users.GetAsync(model.CustomerId.Value,
                cancellationToken: cancellationToken);
            if (user is null ||
                user.Role != UserRole.CUSTOMER)
            {
                throw new ApplicationException("Thông tin người dùng không hợp lệ!!");
            }

            Route route = await work.Routes.GetAsync(model.CustomerRouteId,
                cancellationToken: cancellationToken);
            if (route is null ||
                !route.UserId.Equals(user.Id))
            {
                throw new ApplicationException("Thông tin tuyến đường không hợp lệ!!");
            }

            Route? roundTrip = null;
            if (route.Type == RouteType.ROUND_TRIP)
            {
                if (!route.RoundTripRouteId.HasValue)
                {
                    throw new ApplicationException("Đây là tuyến đường về thuộc tuyến đường khứ hồi! " +
                            "Không thể tạo Booking cho tuyến đường này. Vui lòng tiến hành tạo Booking cho tuyến đường chính!");
                }
                else
                {
                    roundTrip = await work.Routes.GetAsync(route.RoundTripRouteId.Value,
                        cancellationToken: cancellationToken);
                }

                if (model.RoundTripTotalPrice is null || model.RoundTripTotalPrice.Value <= 0 ||
                    model.RoundTripTotalPrice >= model.TotalPrice)
                {
                    throw new ApplicationException("Tổng giá tiền hành trình khứ hồi không hợp lệ!!");
                }
            }

            VehicleType vehicleType = await work.VehicleTypes
                .GetAsync(model.VehicleTypeId, cancellationToken: cancellationToken);
            if (vehicleType is null)
            {
                throw new ApplicationException("Loại phương tiện di chuyển không hợp lệ!!");
            }

            #region Promotion
            //Promotion? promotion = null;
            //if (model.PromotionId.HasValue)
            //{
            //    promotion = await work.Promotions.GetAsync(model.PromotionId.Value,
            //        cancellationToken: cancellationToken);
            //    if (promotion is null ||
            //        promotion.Status == PromotionStatus.UNAVAILABLE ||
            //        (promotion.ExpireTime.HasValue && promotion.ExpireTime < DateTimeUtilities.GetDateTimeVnNow())
            //        || (promotion.StartTime > DateTimeUtilities.GetDateTimeVnNow())
            //        )
            //    {
            //        throw new ApplicationException("Thông tin khuyến mãi không hợp lệ hoặc đã hết hạn sử dụng!");
            //    }

            //    if (!promotion.VehicleTypeId.Equals(vehicleType.Id))
            //    {
            //        throw new ApplicationException("Loại phương tiện và thông tin khuyến mãi không hợp lệ!!");
            //    }
            //    if (promotion.MinTotalPrice.HasValue &&
            //        promotion.MinTotalPrice < model.TotalPrice)
            //    {
            //        throw new ApplicationException("Booking chuyến đi chưa đạt giá trị tối thiểu để áp dụng khuyến mãi!!");
            //    }

            //    // Check Max Usage per user
            //    if (promotion.UsagePerUser.HasValue)
            //    {
            //        int usageCount = (await work.Bookings
            //            .GetAllAsync(query => query.Where(
            //                b => b.CustomerId.Equals(user.Id)
            //                && b.PromotionId.Equals(promotion.Id)),
            //                cancellationToken: cancellationToken)).Count();
            //        if (usageCount > promotion.UsagePerUser.Value)
            //        {
            //            throw new ApplicationException("Mã khuyến mãi đã vượt quá số lần sử dụng!!");
            //        }
            //    }
            //    if (promotion.MaxTotalUsage.HasValue)
            //    {
            //        if (promotion.TotalUsage == promotion.MaxTotalUsage)
            //        {
            //            throw new ApplicationException("Mã khuyến mãi đã vượt quá số lượt sử dụng!!");
            //        }
            //    }

            //}
            #endregion

            //TODO Code
            // Check Start Date and End Date
            //IEnumerable<RouteRoutine> routeRoutines = await
            //    work.RouteRoutines.GetAllAsync(query => query.Where(
            //        r => r.RouteId.Equals(route.Id)), cancellationToken: cancellationToken);

            DateTimeRange newRange = new DateTimeRange(
                model.StartDate, model.EndDate);

            IEnumerable<Booking> currentBookings = await
                work.Bookings.GetAllAsync(query => query.Where(
                    b => b.CustomerRouteId.Equals(model.CustomerRouteId)
                    && b.Status == BookingStatus.CONFIRMED),
                    cancellationToken: cancellationToken);

            foreach (Booking currentBooking in currentBookings)
            {
                DateTimeRange currentRange = new DateTimeRange(
                    currentBooking.StartDate, currentBooking.EndDate);
                newRange.IsOverlap(currentRange, $"Khoảng thời gian bắt đầu và kết thúc " +
                    $"của Booking ({newRange.StartDateTime.Date} - {newRange.EndDateTime.Date}) " +
                    $"đã bị trùng lặp với một Booking khác cho cùng tuyến đường này " +
                    $"({currentRange.StartDateTime.Date} - {currentRange.EndDateTime.Date}");
            }

            IEnumerable<RouteRoutine> routeRoutines = await
                work.RouteRoutines.GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(route.Id)
                    && r.RoutineDate >= model.StartDate
                    && r.RoutineDate <= model.EndDate), cancellationToken: cancellationToken);

            if (!routeRoutines.Any())
            {
                throw new ApplicationException("Không có lịch trình nào thích hợp với thời gian " +
                    "của chuyến đi đã được thiết lập!!");
            }

            IEnumerable<RouteRoutine> roundTripRoutines = new List<RouteRoutine>();
            if (roundTrip != null)
            {
                roundTripRoutines = await work.RouteRoutines.GetAllAsync(
                    query => query.Where(r => r.RouteId.Equals(roundTrip.Id)
                    && r.RoutineDate >= model.StartDate
                    && r.RoutineDate <= model.EndDate), cancellationToken: cancellationToken);

                if (!roundTripRoutines.Any())
                {
                    throw new ApplicationException("Không có lịch trình nào của chuyến về thích hợp với thời gian " +
                    "của chuyến đi đã được thiết lập!!");
                }
                //routeRoutines = routeRoutines.Concat(roundTripRoutines);
            }

            int bookingDetailCount = routeRoutines.Count() + roundTripRoutines.Count();

            //if (model.Type == BookingType.ROUND_TRIP)
            //{
            //    bookingDetailCount *= 2;
            //}

            if (bookingDetailCount == 0)
            {
                throw new ApplicationException("Không có lịch trình nào thích hợp với thời gian " +
                    "của chuyến đi đã được thiết lập!!");
            }

            //if (model.Type == BookingType.ROUND_TRIP && model.CustomerRoundTripDesiredPickupTime is null)
            //{
            //    throw new ApplicationException("Thông tin thời gian đón khách cho chuyến về chưa được thiết lập!!!");
            //}
            DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();

            if ((model.StartDate - vnNow).TotalHours < 24)
            {
                throw new ApplicationException("Các hành trình phải được đặt trước ít nhất 24 tiếng trước so với ngày bắt đầu!");
            }

            // All is valid
            Booking booking = new Booking()
            {
                CustomerId = model.CustomerId.Value,
                CustomerRouteId = model.CustomerRouteId,
                StartDate = model.StartDate,
                DaysOfWeek = model.DaysOfWeek,
                EndDate = model.EndDate,
                TotalPrice = model.TotalPrice,
                PriceAfterDiscount = model.PriceAfterDiscount,
                IsShared = model.IsShared,
                Duration = Math.Round(model.Duration, 2),
                Distance = model.Distance,
                //PromotionId = model.PromotionId,
                VehicleTypeId = model.VehicleTypeId,
                Type = route.Type == RouteType.ONE_WAY
                    ? BookingType.ONE_WAY
                    : BookingType.ROUND_TRIP,
                //Type = model.Type,
                //Status = BookingStatus.DRAFT // TODO Code
                Status = BookingStatus.CONFIRMED
            };

            await work.Bookings.InsertAsync(booking,
                cancellationToken: cancellationToken);

            Wallet customerWallet = await work.Wallets.GetAsync(
                w => w.UserId.Equals(model.CustomerId), cancellationToken: cancellationToken);
            // TODO Code
            if (customerWallet.Balance < model.TotalPrice)
            {
                throw new ApplicationException("Số dư ví không đủ để thực hiện tạo hành trình! Vui lòng nạp thêm tiền");
            }

            //if (customerWallet.Balance >= model.TotalPrice)
            //{
            //booking.Status = BookingStatus.CONFIRMED;
            WalletTransaction customerTransaction = new WalletTransaction
            {
                WalletId = customerWallet.Id,
                Amount = model.TotalPrice,
                BookingId = booking.Id,
                PaymentMethod = PaymentMethod.WALLET,
                Type = WalletTransactionType.BOOKING_PAID,
                Status = WalletTransactionStatus.SUCCESSFULL,
            };
            customerWallet.Balance -= model.TotalPrice;

            Wallet systemWallet = await work.Wallets.GetAsync(
                w => w.Type == WalletType.SYSTEM,
                cancellationToken: cancellationToken);
            WalletTransaction systemTransaction = new WalletTransaction
            {
                WalletId = systemWallet.Id,
                Amount = model.TotalPrice,
                BookingId = booking.Id,
                PaymentMethod = PaymentMethod.WALLET,
                Type = WalletTransactionType.BOOKING_PAID,
                Status = WalletTransactionStatus.SUCCESSFULL
            };

            systemWallet.Balance += model.TotalPrice;

            await work.WalletTransactions.InsertAsync(customerTransaction, cancellationToken: cancellationToken);
            await work.WalletTransactions.InsertAsync(systemTransaction, cancellationToken: cancellationToken);

            await work.Wallets.UpdateAsync(customerWallet);
            await work.Wallets.UpdateAsync(systemWallet);

            //}

            //if ((await IsEnoughWalletBalanceToBook(model, cancellationToken)))
            //{
            //    booking.Status = BookingStatus.CONFIRMED;
            //}

            // Generate BookingDetails
            double mainTripPrice = model.TotalPrice;
            double roundTripPrice = 0;
            if (booking.Type == BookingType.ROUND_TRIP)
            {
                roundTripPrice = model.RoundTripTotalPrice.Value;
                mainTripPrice = model.TotalPrice - model.RoundTripTotalPrice.Value;
            }

            double priceEachTrip = Math.Round(mainTripPrice / routeRoutines.Count(), 0);
            double priceAfterDiscountEachTrip = Math.Round(mainTripPrice / routeRoutines.Count(), 0);

            double priceEachRoundTrip = Math.Round(roundTripPrice / roundTripRoutines.Count(), 0);

            FareServices fareServices = new FareServices(work, _logger);

            foreach (RouteRoutine routeRoutine in routeRoutines)
            {
                // Main Route
                BookingDetail bookingDetail = new BookingDetail
                {
                    BookingId = booking.Id,
                    Date = routeRoutine.RoutineDate,
                    Price = priceEachTrip,
                    PriceAfterDiscount = priceAfterDiscountEachTrip,
                    CustomerRouteRoutineId = routeRoutine.Id,
                    StartStationId = route.StartStationId,
                    EndStationId = route.EndStationId,
                    CustomerDesiredPickupTime = routeRoutine.PickupTime,
                    DriverWage = await fareServices.CalculateDriverWage(priceEachTrip, cancellationToken),
                    Status = BookingDetailStatus.PENDING_ASSIGN,
                    Type = BookingDetailType.MAIN_ROUTE
                };
                await work.BookingDetails.InsertAsync(bookingDetail,
                    cancellationToken: cancellationToken);

                //if (model.Type == BookingType.ROUND_TRIP)
                //{
                //    BookingDetail roundBookingDetail = new BookingDetail
                //    {
                //        BookingId = booking.Id,
                //        Date = routeRoutine.RoutineDate,
                //        Price = priceEachTrip,
                //        PriceAfterDiscount = priceAfterDiscountEachTrip,
                //        CustomerRouteRoutineId = routeRoutine.Id,
                //        StartStationId = route.EndStationId,
                //        EndStationId = route.StartStationId,
                //        DriverWage = await fareServices.CalculateDriverWage(priceEachTrip, cancellationToken),
                //        CustomerDesiredPickupTime = model.CustomerRoundTripDesiredPickupTime.Value.ToTimeSpan(),
                //        Status = BookingDetailStatus.PENDING_ASSIGN
                //    };
                //    await work.BookingDetails.InsertAsync(bookingDetail,
                //        cancellationToken: cancellationToken);
                //}
            }

            if (roundTripRoutines.Any())
            {
                foreach (RouteRoutine routine in roundTripRoutines)
                {
                    // RoundTrip Route
                    BookingDetail bookingDetail = new BookingDetail
                    {
                        BookingId = booking.Id,
                        Date = routine.RoutineDate,
                        Price = priceEachRoundTrip,
                        PriceAfterDiscount = priceEachRoundTrip,
                        CustomerRouteRoutineId = routine.Id,
                        StartStationId = roundTrip.StartStationId,
                        EndStationId = roundTrip.EndStationId,
                        CustomerDesiredPickupTime = routine.PickupTime,
                        DriverWage = await fareServices.CalculateDriverWage(priceEachRoundTrip, cancellationToken),
                        Status = BookingDetailStatus.PENDING_ASSIGN,
                        Type = BookingDetailType.ROUND_TRIP_ROUTE
                    };
                    await work.BookingDetails.InsertAsync(bookingDetail,
                        cancellationToken: cancellationToken);
                }
            }


            //if (promotion != null)
            //{
            //    promotion.TotalUsage += 1;
            //    await work.Promotions.UpdateAsync(promotion);
            //}

            await work.SaveChangesAsync(cancellationToken);
            return booking;
        }

        public async Task<Booking> UpdateBookingAsync(RouteBookingUpdateModel routeBookingUpdateModel,
            CancellationToken cancellationToken)
        {
            #region Route Validation
            Route route = await work.Routes.GetAsync(routeBookingUpdateModel.RouteId, cancellationToken: cancellationToken);
            if (route is null)
            {
                throw new ApplicationException("Không tìm thấy tuyến đường! Vui lòng kiểm tra lại thông tin");
            }

            if (route.Type == RouteType.ROUND_TRIP
                && !route.RoundTripRouteId.HasValue)
            {
                // The roundtrip route
                throw new ApplicationException("Vui lòng cập nhật thông tin của tuyến đường chính!");
            }

            Booking? booking = await work.Bookings.GetAsync(
                    b => b.Id.Equals(routeBookingUpdateModel.BookingUpdate.BookingId)
                    && b.CustomerRouteId.Equals(routeBookingUpdateModel.RouteId)
                    && b.Status == BookingStatus.CONFIRMED, cancellationToken: cancellationToken);
            if (booking is null)
            {
                throw new ApplicationException("Thông tin tuyến đường và đặt lịch không phù hợp! Vui lòng kiểm tra lại!");
            }
            if (routeBookingUpdateModel.RouteRoutines is null || routeBookingUpdateModel.RouteRoutines.Count == 0)
            {
                throw new ApplicationException("Thiếu thông tin lịch trình! Vui lòng kiểm tra lại");
            }
            if (routeBookingUpdateModel.Type == RouteType.ROUND_TRIP)
            {
                if (routeBookingUpdateModel.RoundTripRoutines is null || routeBookingUpdateModel.RoundTripRoutines.Count == 0)
                {
                    throw new ApplicationException("Không tìm thấy thông tin lịch trình cho chuyến về! Vui lòng kiểm tra lại");
                }

                IsValidRoundTripRoutines(routeBookingUpdateModel.RouteRoutines,
                    routeBookingUpdateModel.RoundTripRoutines);
            }

            #endregion
            #region Validation Booking
            IEnumerable<BookingDetail> currentDetails = await work.BookingDetails
                    .GetAllAsync(query => query.Where(
                        d => d.BookingId.Equals(booking.Id)), cancellationToken: cancellationToken);

            if (currentDetails.Any(d => d.DriverId.HasValue))
            {
                throw new ApplicationException("Hành trình đã có tài xế chọn, không thể tiến hành cập nhật!");
            }

            BookingUpdateModel bookingUpdate = routeBookingUpdateModel.BookingUpdate;
            if (bookingUpdate.Distance <= 0)
            {
                throw new ApplicationException("Khoảng cách phải lớn hơn 0!");
            }
            if (bookingUpdate.Duration <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển phải lớn hơn 0!");
            }

            if (routeBookingUpdateModel.Type == RouteType.ROUND_TRIP)
            {
                if (bookingUpdate.RoundTripTotalPrice is null || bookingUpdate.RoundTripTotalPrice.Value <= 0 ||
                    bookingUpdate.RoundTripTotalPrice >= bookingUpdate.TotalPrice)
                {
                    throw new ApplicationException("Tổng giá tiền hành trình khứ hồi không hợp lệ!!");
                }
            }
            //VehicleType vehicleType = await work.VehicleTypes
            //    .GetAsync(bookingUpdate.VehicleTypeId, cancellationToken: cancellationToken);
            //if (vehicleType is null)
            //{
            //    throw new ApplicationException("Loại phương tiện di chuyển không hợp lệ!!");
            //}

            DateTimeRange newRange = new DateTimeRange(
                bookingUpdate.StartDate, bookingUpdate.EndDate);

            IEnumerable<Booking> currentBookings = await
                work.Bookings.GetAllAsync(query => query.Where(
                    b => b.CustomerRouteId.Equals(booking.CustomerRouteId)
                    && b.Status == BookingStatus.CONFIRMED
                    && !b.Id.Equals(booking.Id)), // Except for the updated booking
                    cancellationToken: cancellationToken);
            foreach (Booking currentBooking in currentBookings)
            {
                DateTimeRange currentRange = new DateTimeRange(
                    currentBooking.StartDate, currentBooking.EndDate);
                newRange.IsOverlap(currentRange, $"Khoảng thời gian bắt đầu và kết thúc " +
                    $"của Booking ({newRange.StartDateTime.Date} - {newRange.EndDateTime.Date}) " +
                    $"đã bị trùng lặp với một Booking khác cho cùng tuyến đường này " +
                    $"({currentRange.StartDateTime.Date} - {currentRange.EndDateTime.Date}");
            }

            #endregion

            // TODO Code
            // Booking information to update is valid
            // Update Route
            using var transaction = await work.BeginTransactionAsync(cancellationToken);
            try
            {
                // Delete current BookingDetails

                foreach (BookingDetail detail in currentDetails)
                {
                    await work.BookingDetails.DeleteAsync(detail, isSoftDelete: true,
                         cancellationToken);
                }

                RouteServices routeServices = new RouteServices(work, _logger);
                RouteUpdateModel routeUpdateModel = new RouteUpdateModel()
                {
                    Id = routeBookingUpdateModel.RouteId,
                    UserId = routeBookingUpdateModel.UserId,
                    Name = routeBookingUpdateModel.Name,
                    Distance = routeBookingUpdateModel.Distance,
                    Duration = routeBookingUpdateModel.Duration,
                    Status = routeBookingUpdateModel.Status,
                    RoutineType = routeBookingUpdateModel.RoutineType,
                    Type = routeBookingUpdateModel.Type,
                    StartStation = routeBookingUpdateModel.StartStation,
                    EndStation = routeBookingUpdateModel.EndStation
                };

                Route updatedRoute = await routeServices.UpdateRouteAsync(
                    routeUpdateModel,
                    isCalledFromBooking: true,
                    cancellationToken);

                RouteRoutineServices routeRoutineServices = new RouteRoutineServices(work, _logger);

                RouteRoutineUpdateModel routeRoutineUpdateModel = new RouteRoutineUpdateModel()
                {
                    RouteId = routeBookingUpdateModel.RouteId,
                    RouteRoutines = routeBookingUpdateModel.RouteRoutines
                };

                IEnumerable<RouteRoutine> updatedRoutines = await routeRoutineServices
                    .UpdateRouteRoutinesAsync(routeRoutineUpdateModel, false, cancellationToken);

                RouteRoutineUpdateModel? roundTripRoutineUpdateModel = null;
                IEnumerable<RouteRoutine>? updatedRoundTripRoutines = null;

                Route? roundTrip = null;
                if (updatedRoute.Type == RouteType.ROUND_TRIP)
                {
                    roundTrip = await work.Routes.GetAsync(updatedRoute.RoundTripRouteId.Value,
                        cancellationToken: cancellationToken);
                }

                if (roundTrip != null)
                {
                    roundTripRoutineUpdateModel = new RouteRoutineUpdateModel()
                    {
                        RouteId = roundTrip.Id,
                        RouteRoutines = routeBookingUpdateModel.RoundTripRoutines
                    };
                    updatedRoundTripRoutines = await routeRoutineServices
                        .UpdateRouteRoutinesAsync(roundTripRoutineUpdateModel, false, cancellationToken);
                }

                IEnumerable<RouteRoutine> routeRoutines = await
                    work.RouteRoutines.GetAllAsync(query => query.Where(
                        r => r.RouteId.Equals(updatedRoute.Id)
                        && r.RoutineDate >= bookingUpdate.StartDate
                        && r.RoutineDate <= bookingUpdate.EndDate), cancellationToken: cancellationToken);

                if (!routeRoutines.Any())
                {
                    throw new ApplicationException("Không có lịch trình nào thích hợp với thời gian " +
                        "của chuyến đi đã được thiết lập!!");
                }

                IEnumerable<RouteRoutine> roundTripRoutines = new List<RouteRoutine>();
                if (roundTrip != null)
                {
                    roundTripRoutines = await work.RouteRoutines.GetAllAsync(
                        query => query.Where(r => r.RouteId.Equals(roundTrip.Id)
                        && r.RoutineDate >= bookingUpdate.StartDate
                        && r.RoutineDate <= bookingUpdate.EndDate), cancellationToken: cancellationToken);

                    if (!roundTripRoutines.Any())
                    {
                        throw new ApplicationException("Không có lịch trình nào của chuyến về thích hợp với thời gian " +
                        "của chuyến đi đã được thiết lập!!");
                    }
                    //routeRoutines = routeRoutines.Concat(roundTripRoutines);
                }

                int bookingDetailCount = routeRoutines.Count() + roundTripRoutines.Count();

                //if (model.Type == BookingType.ROUND_TRIP)
                //{
                //    bookingDetailCount *= 2;
                //}

                if (bookingDetailCount == 0)
                {
                    throw new ApplicationException("Không có lịch trình nào thích hợp với thời gian " +
                        "của chuyến đi đã được thiết lập!!");
                }

                double oldTotalPrice = booking.TotalPrice.Value;
                double newTotalPrice = bookingUpdate.TotalPrice;
                double difference = newTotalPrice - oldTotalPrice;
                double differenceAmount = Math.Abs(difference);

                await work.Bookings.DetachAsync(booking);

                booking.StartDate = bookingUpdate.StartDate;
                booking.EndDate = bookingUpdate.EndDate;
                booking.DaysOfWeek = bookingUpdate.DaysOfWeek;
                booking.TotalPrice = bookingUpdate.TotalPrice;
                booking.PriceAfterDiscount = bookingUpdate.PriceAfterDiscount;
                booking.IsShared = bookingUpdate.IsShared;
                booking.Duration = Math.Round(bookingUpdate.Duration, 2);
                booking.Distance = bookingUpdate.Distance;
                booking.Type = updatedRoute.Type == RouteType.ONE_WAY ?
                    BookingType.ONE_WAY : BookingType.ROUND_TRIP;

                await work.Bookings.UpdateAsync(booking);

                if (difference != 0)
                {
                    Wallet customerWallet = await work.Wallets.GetAsync(
                        w => w.UserId.Equals(booking.CustomerId), cancellationToken: cancellationToken);

                    if (difference > 0 && customerWallet.Balance < difference)
                    {
                        throw new ApplicationException("Số dư ví không đủ để thực hiện chỉnh sửa hành trình! Vui lòng nạp thêm tiền");
                    }

                    //if (customerWallet.Balance >= model.TotalPrice)
                    //{
                    //booking.Status = BookingStatus.CONFIRMED;
                    WalletTransaction customerTransaction = new WalletTransaction
                    {
                        WalletId = customerWallet.Id,
                        Amount = differenceAmount,
                        BookingId = booking.Id,
                        PaymentMethod = PaymentMethod.WALLET,
                        Type = difference > 0 ? WalletTransactionType.BOOKING_PAID : WalletTransactionType.BOOKING_REFUND,
                        Status = WalletTransactionStatus.SUCCESSFULL,
                    };
                    if (difference > 0)
                    {
                        customerWallet.Balance -= differenceAmount;

                    }
                    else
                    {
                        // Refund
                        customerWallet.Balance += differenceAmount;
                    }


                    Wallet systemWallet = await work.Wallets.GetAsync(
                        w => w.Type == WalletType.SYSTEM,
                        cancellationToken: cancellationToken);
                    WalletTransaction systemTransaction = new WalletTransaction
                    {
                        WalletId = systemWallet.Id,
                        Amount = differenceAmount,
                        BookingId = booking.Id,
                        PaymentMethod = PaymentMethod.WALLET,
                        Type = difference > 0 ? WalletTransactionType.BOOKING_PAID : WalletTransactionType.BOOKING_REFUND,
                        Status = WalletTransactionStatus.SUCCESSFULL
                    };

                    if (difference > 0)
                    {
                        systemWallet.Balance += differenceAmount;
                    }
                    else
                    {
                        // Refund
                        systemWallet.Balance -= differenceAmount;
                    }

                    await work.WalletTransactions.InsertAsync(customerTransaction, cancellationToken: cancellationToken);
                    await work.WalletTransactions.InsertAsync(systemTransaction, cancellationToken: cancellationToken);

                    await work.Wallets.DetachAsync(customerWallet);
                    await work.Wallets.DetachAsync(systemWallet);

                    await work.Wallets.UpdateAsync(customerWallet);
                    await work.Wallets.UpdateAsync(systemWallet);
                }

                //}

                //if ((await IsEnoughWalletBalanceToBook(model, cancellationToken)))
                //{
                //    booking.Status = BookingStatus.CONFIRMED;
                //}

                // Generate BookingDetails
                double mainTripPrice = bookingUpdate.TotalPrice;
                double roundTripPrice = 0;
                if (booking.Type == BookingType.ROUND_TRIP)
                {
                    roundTripPrice = bookingUpdate.RoundTripTotalPrice.Value;
                    mainTripPrice = bookingUpdate.TotalPrice - bookingUpdate.RoundTripTotalPrice.Value;
                }

                double priceEachTrip = Math.Round(mainTripPrice / routeRoutines.Count(), 0);
                double priceAfterDiscountEachTrip = Math.Round(mainTripPrice / routeRoutines.Count(), 0);

                double priceEachRoundTrip = Math.Round(roundTripPrice / roundTripRoutines.Count(), 0);

                FareServices fareServices = new FareServices(work, _logger);

                foreach (RouteRoutine routeRoutine in routeRoutines)
                {
                    // Main Route
                    BookingDetail bookingDetail = new BookingDetail
                    {
                        BookingId = booking.Id,
                        Date = routeRoutine.RoutineDate,
                        Price = priceEachTrip,
                        PriceAfterDiscount = priceAfterDiscountEachTrip,
                        CustomerRouteRoutineId = routeRoutine.Id,
                        StartStationId = updatedRoute.StartStationId,
                        EndStationId = updatedRoute.EndStationId,
                        CustomerDesiredPickupTime = routeRoutine.PickupTime,
                        DriverWage = await fareServices.CalculateDriverWage(priceEachTrip, cancellationToken),
                        Status = BookingDetailStatus.PENDING_ASSIGN,
                        Type = BookingDetailType.MAIN_ROUTE
                    };
                    await work.BookingDetails.InsertAsync(bookingDetail,
                        cancellationToken: cancellationToken);

                    //if (model.Type == BookingType.ROUND_TRIP)
                    //{
                    //    BookingDetail roundBookingDetail = new BookingDetail
                    //    {
                    //        BookingId = booking.Id,
                    //        Date = routeRoutine.RoutineDate,
                    //        Price = priceEachTrip,
                    //        PriceAfterDiscount = priceAfterDiscountEachTrip,
                    //        CustomerRouteRoutineId = routeRoutine.Id,
                    //        StartStationId = route.EndStationId,
                    //        EndStationId = route.StartStationId,
                    //        DriverWage = await fareServices.CalculateDriverWage(priceEachTrip, cancellationToken),
                    //        CustomerDesiredPickupTime = model.CustomerRoundTripDesiredPickupTime.Value.ToTimeSpan(),
                    //        Status = BookingDetailStatus.PENDING_ASSIGN
                    //    };
                    //    await work.BookingDetails.InsertAsync(bookingDetail,
                    //        cancellationToken: cancellationToken);
                    //}
                }

                if (roundTripRoutines.Any())
                {
                    foreach (RouteRoutine routine in roundTripRoutines)
                    {
                        // RoundTrip Route
                        BookingDetail bookingDetail = new BookingDetail
                        {
                            BookingId = booking.Id,
                            Date = routine.RoutineDate,
                            Price = priceEachRoundTrip,
                            PriceAfterDiscount = priceEachRoundTrip,
                            CustomerRouteRoutineId = routine.Id,
                            StartStationId = updatedRoute.EndStationId,
                            EndStationId = updatedRoute.StartStationId,
                            CustomerDesiredPickupTime = routine.PickupTime,
                            DriverWage = await fareServices.CalculateDriverWage(priceEachRoundTrip, cancellationToken),
                            Status = BookingDetailStatus.PENDING_ASSIGN,
                            Type = BookingDetailType.ROUND_TRIP_ROUTE
                        };
                        await work.BookingDetails.InsertAsync(bookingDetail,
                            cancellationToken: cancellationToken);
                    }
                }

                await work.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return booking;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                await work.FlushAllRedisAsync(cancellationToken);
                throw;
            }
        }

        public async Task<(Booking, Guid?, int, bool, IEnumerable<Guid>)> CancelBookingAsync(Guid bookingId,
            CancellationToken cancellationToken)
        {
            Booking? booking = await work.Bookings
                .GetAsync(bookingId, cancellationToken: cancellationToken);

            if (booking is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!!");
            }

            User? cancelledUser = null;
            int inWeekCount = 0;

            bool isDriverPaid = false;
            IEnumerable<Guid> completedBookingDetailIds = new List<Guid>();

            if (booking.Status == BookingStatus.CANCELED_BY_BOOKER)
            {
                return (booking, null, 0, false, completedBookingDetailIds);
            }

            if (!IdentityUtilities.IsAdmin())
            {
                Guid currentId = IdentityUtilities.GetCurrentUserId();

                if (!currentId.Equals(booking.CustomerId))
                {
                    // Not the accessible user
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!");
                }

                // Customer cancels the bookingdetail
                cancelledUser = await work.Users.GetAsync(booking.CustomerId,
                    cancellationToken: cancellationToken);
            }

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.BookingId.Equals(booking.Id)
                    && bd.Status != BookingDetailStatus.CANCELLED),
                cancellationToken: cancellationToken);

            DateTime now = DateTimeUtilities.GetDateTimeVnNow();

            bookingDetails = bookingDetails.Where(
                d =>
                {
                    DateTime pickupDateTime = DateOnly.FromDateTime(d.Date)
                        .ToDateTime(TimeOnly.FromTimeSpan(d.CustomerDesiredPickupTime));
                    return pickupDateTime >= now;
                });

            if (bookingDetails.Count() > 0)
            {
                bookingDetails = bookingDetails.OrderBy(bd => bd.PickupTime);

                #region Old
                //BookingDetail firstPickup = bookingDetails.First();

                //DateTime pickupDateTime = DateOnly.FromDateTime(firstPickup.Date)
                //    .ToDateTime(TimeOnly.FromTimeSpan(firstPickup.CustomerDesiredPickupTime));

                //if (cancelledUser != null)
                //{
                //    int tripsInWeek = bookingDetails.Count(
                //        d => d.Date.IsInCurrentWeek());

                //    if (cancelledUser.WeeklyCanceledTripRate + tripsInWeek > 3)
                //    {
                //        throw new ApplicationException("Số chuyến đi được phép hủy có lịch trình trong tuần này của bạn " +
                //                "đã đạt giới hạn (3 chuyến đi). Bạn không thể hủy thêm chuyến đi nào có lịch trình " +
                //                "trong tuần này nữa!");
                //    }

                //    inWeekCount = tripsInWeek;
                //}

                //double chargeFee = 0;

                //if (!firstPickup.DriverId.HasValue)
                //{
                //    // No driver
                //    // No fee
                //    chargeFee = 0;
                //}
                //else
                //{
                //    // Has driver
                //    TimeSpan difference = pickupDateTime - now;

                //    if (difference.TotalHours >= 6)
                //    {
                //        chargeFee = 0;
                //    }
                //    else if (difference.TotalHours >= 1)
                //    {
                //        // < 6 hours and >= 1 hour
                //        //chargeFee = calculateChargeFee(bookingDetails, 0.2, 0.15);
                //        chargeFee = 0.1;
                //    }
                //    else
                //    {
                //        //chargeFee = calculateChargeFee(bookingDetails, 0.7, 0.6);
                //        chargeFee = 1;
                //    }
                //}

                //double chargeFeeAmount = firstPickup.PriceAfterDiscount.Value * chargeFee;
                //chargeFeeAmount = FareUtilities.RoundToThousands(chargeFeeAmount);

                //if (cancelledUser != null && chargeFeeAmount > 0)
                //{
                //    // User is customer
                //    Wallet wallet = await work.Wallets.GetAsync(
                //        w => w.UserId.Equals(cancelledUser.Id),
                //        cancellationToken: cancellationToken);

                //    WalletTransaction walletTransaction = new WalletTransaction
                //    {
                //        WalletId = wallet.Id,
                //        BookingId = booking.Id,
                //        Amount = chargeFeeAmount,
                //        Type = WalletTransactionType.CANCEL_FEE,
                //        Status = WalletTransactionStatus.PENDING
                //    };

                //    await work.WalletTransactions.InsertAsync(walletTransaction,
                //        cancellationToken: cancellationToken);

                //    if (wallet.Balance >= chargeFeeAmount)
                //    {
                //        // Able to be substracted the amount
                //        wallet.Balance -= chargeFeeAmount;

                //        walletTransaction.Status = WalletTransactionStatus.SUCCESSFULL;

                //        Wallet systemWallet = await work.Wallets.GetAsync(
                //            w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);
                //        WalletTransaction systemTransaction = new WalletTransaction
                //        {
                //            WalletId = systemWallet.Id,
                //            BookingId = booking.Id,
                //            Amount = chargeFeeAmount,
                //            Type = WalletTransactionType.CANCEL_FEE,
                //            Status = WalletTransactionStatus.SUCCESSFULL
                //        };

                //        systemWallet.Balance += chargeFeeAmount;

                //        await work.WalletTransactions.InsertAsync(systemTransaction,
                //        cancellationToken: cancellationToken);
                //        await work.Wallets.UpdateAsync(systemWallet);

                //        await work.Wallets.UpdateAsync(wallet);
                //    }
                //}
                #endregion

                if (cancelledUser != null)
                {
                    int tripsInWeek = bookingDetails.Count(
                        d => d.Date.IsInCurrentWeek());

                    if (cancelledUser.WeeklyCanceledTripRate + tripsInWeek > 3)
                    {
                        throw new ApplicationException("Số chuyến đi được phép hủy có lịch trình trong tuần này của bạn " +
                                "đã đạt giới hạn (3 chuyến đi). Bạn không thể hủy thêm chuyến đi nào có lịch trình " +
                                "trong tuần này nữa!");
                    }

                    inWeekCount = tripsInWeek;
                }

                Wallet systemWallet = await work.Wallets.GetAsync(
                            w => w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);

                Wallet? customerWallet = null;
                if (cancelledUser != null)
                {
                    customerWallet = await work.Wallets.GetAsync(
                        w => w.UserId.Equals(cancelledUser.Id),
                        cancellationToken: cancellationToken);
                }

                IEnumerable<Guid> driverIds = bookingDetails.Where(
                    d => d.DriverId.HasValue).Select(d => d.DriverId.Value)
                    .Distinct();

                IEnumerable<Wallet> driverWallets = await work.Wallets
                    .GetAllAsync(query => query.Where(
                        w => driverIds.Contains(w.UserId)), cancellationToken: cancellationToken);

                foreach (BookingDetail bookingDetail in bookingDetails)
                {
                    bookingDetail.Status = BookingDetailStatus.CANCELLED;
                    bookingDetail.CanceledUserId = IdentityUtilities.GetCurrentUserId();

                    await work.BookingDetails.UpdateAsync(bookingDetail);

                    if (cancelledUser != null)
                    {
                        // Customer

                        DateTime pickupDateTime = DateOnly.FromDateTime(bookingDetail.Date)
                            .ToDateTime(TimeOnly.FromTimeSpan(bookingDetail.CustomerDesiredPickupTime));
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
                            WalletTransaction walletTransaction = new WalletTransaction
                            {
                                WalletId = customerWallet.Id,
                                BookingDetailId = bookingDetail.Id,
                                BookingId = booking.Id,
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
                                BookingId = booking.Id,
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

                        if (bookingDetail.DriverId.HasValue)
                        {
                            // Refund to Driver
                            //Wallet driverWallet = await work.Wallets.GetAsync(
                            //    w => w.UserId.Equals(bookingDetail.DriverId.Value),
                            //    cancellationToken: cancellationToken);
                            Wallet driverWallet = driverWallets.SingleOrDefault(
                                w => w.UserId.Equals(bookingDetail.DriverId.Value));

                            if (chargeFee == 1)
                            {
                                // Trip was considered completed, Driver gets paid for the whole trip
                                isDriverPaid = true;
                                //customerId = cancelledUser.Id;
                                completedBookingDetailIds = completedBookingDetailIds.Append(bookingDetail.Id);
                            }
                            else
                            {
                                WalletTransaction driverPickDetailTransaction = await work.WalletTransactions
                                    .GetAsync(t => t.WalletId.Equals(driverWallet.Id)
                                    && t.BookingDetailId.Equals(bookingDetail.Id)
                                    && t.Type == WalletTransactionType.TRIP_PICK, cancellationToken: cancellationToken);

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

                    }

                }
            }

            // TODO Code for Cancelling Policy
            //if (now > pickupDateTime)
            //{
            //    throw new ApplicationException("Chuyến đi trong quá khứ, không thể thực hiện hủy chuyến đi!");
            //}

            booking.Status = BookingStatus.CANCELED_BY_BOOKER;

            await work.Bookings.UpdateAsync(booking);

            await work.SaveChangesAsync(cancellationToken);

            // Send notification to Driver and Customer

            User customer = await work.Users.GetAsync(booking.CustomerId, cancellationToken: cancellationToken);

            string? customerFcm = customer.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.Booking },
                { "bookingId", booking.Id.ToString() },
            };

            if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
            {

                NotificationCreateModel customerNotification = new NotificationCreateModel()
                {
                    UserId = customer.Id,
                    Title = "Hành trình đã bị hủy!",
                    Description = $"Các chuyến đi trong hành trình của bạn đã " +
                    $"được hủy thành công!!",
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

            // Send to driver of each Booking Detail
            if (bookingDetails.Any())
            {
                IEnumerable<BookingDetail> assignedDetails = bookingDetails.Where(
                d => d.DriverId.HasValue);
                IEnumerable<Guid> stationIds = assignedDetails.Select(
                    d => d.StartStationId).Concat(assignedDetails.Select(d => d.EndStationId))
                    .Distinct();
                IEnumerable<Station> stations = await work.Stations
                    .GetAllAsync(query => query.Where(s => stationIds.Contains(s.Id)),
                    includeDeleted: true,
                    cancellationToken: cancellationToken);

                foreach (BookingDetail detail in assignedDetails)
                {
                    if (detail.DriverId.HasValue)
                    {
                        User driver = await work.Users.GetAsync(
                            detail.DriverId.Value, cancellationToken: cancellationToken);
                        string? driverFcm = driver.FcmToken;
                        if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
                        {
                            Station startStation = stations.SingleOrDefault(
                                s => s.Id.Equals(detail.StartStationId));
                            Station endStation = stations.SingleOrDefault(
                                s => s.Id.Equals(detail.EndStationId));

                            Dictionary<string, string> dataToSendDriver = new Dictionary<string, string>()
                        {
                            {"action", NotificationAction.BookingDetail },
                            { "bookingDetailId", detail.Id.ToString() },
                        };

                            NotificationCreateModel driverNotification = new NotificationCreateModel()
                            {
                                UserId = driver.Id,
                                Title = "Chuyến đi đã bị hủy!",
                                Description = $"{detail.PickUpDateTimeString()}, từ " +
                                    $"{startStation.Name} đến {endStation.Name}",
                                Type = NotificationType.SPECIFIC_USER
                            };

                            await notificationServices.CreateFirebaseNotificationAsync(
                                driverNotification, driverFcm, dataToSendDriver, cancellationToken);
                        }
                    }
                }
            }

            return (booking, cancelledUser?.Id, inWeekCount, isDriverPaid, completedBookingDetailIds);

            //double calculateChargeFee(IEnumerable<BookingDetail> bookingDetails,
            //    double chargePercent)
            //{
            //    double fee = 0;
            //    foreach (BookingDetail bookingDetail in bookingDetails)
            //    {

            //            fee += bookingDetail.PriceAfterDiscount.Value * driverSelectedChargePercent;

            //    }

            //    return fee;
            //}
        }

        public async Task<double> CalculateCancelBookingFeeAsync(
            Guid bookingId, CancellationToken cancellationToken)
        {
            Booking? booking = await work.Bookings
                .GetAsync(bookingId, cancellationToken: cancellationToken);

            if (booking is null)
            {
                throw new ApplicationException("Chuyến đi không tồn tại!!!");
            }

            //User? cancelledUser = null;

            //bool isDriverPaid = false;
            IEnumerable<Guid> completedBookingDetailIds = new List<Guid>();

            if (booking.Status == BookingStatus.CANCELED_BY_BOOKER)
            {
                return 0;
            }

            if (!IdentityUtilities.IsAdmin())
            {
                Guid currentId = IdentityUtilities.GetCurrentUserId();

                if (!currentId.Equals(booking.CustomerId))
                {
                    // Not the accessible user
                    throw new AccessDeniedException("Bạn không thể thực hiện hành động này!");
                }

                //// Customer cancels the bookingdetail
                //cancelledUser = await work.Users.GetAsync(booking.CustomerId,
                //    cancellationToken: cancellationToken);
            }

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.BookingId.Equals(booking.Id)
                    && bd.Status != BookingDetailStatus.CANCELLED),
                cancellationToken: cancellationToken);

            DateTime now = DateTimeUtilities.GetDateTimeVnNow();

            bookingDetails = bookingDetails.Where(
                d =>
                {
                    DateTime pickupDateTime = DateOnly.FromDateTime(d.Date)
                        .ToDateTime(TimeOnly.FromTimeSpan(d.CustomerDesiredPickupTime));
                    return pickupDateTime >= now;
                });

            double cancelFee = 0;

            if (bookingDetails.Count() > 0)
            {
                bookingDetails = bookingDetails.OrderBy(bd => bd.PickupTime);

                foreach (BookingDetail bookingDetail in bookingDetails)
                {
                    DateTime pickupDateTime = DateOnly.FromDateTime(bookingDetail.Date)
                        .ToDateTime(TimeOnly.FromTimeSpan(bookingDetail.CustomerDesiredPickupTime));
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

                    double chargeFeeAmount = bookingDetail.Price.Value * chargeFee;
                    cancelFee += FareUtilities.RoundToThousands(chargeFeeAmount);
                    //double customerRefundAmount = bookingDetail.Price.Value * (1 - chargeFee);
                    //    customerRefundAmount = FareUtilities.RoundToThousands(customerRefundAmount);

                }
            }

            return cancelFee;
        }

        public async Task<BookingAnalysisModel> GetBookingAnalysisAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Booking> bookings;
            if (IdentityUtilities.IsAdmin())
            {
                // Admin
                bookings = await work.Bookings.GetAllAsync(cancellationToken: cancellationToken);
            }
            else
            {
                // Customer
                Guid customerId = IdentityUtilities.GetCurrentUserId();
                bookings = await work.Bookings.GetAllAsync(
                    query => query.Where(b => b.CustomerId.Equals(customerId)),
                    cancellationToken: cancellationToken);
            }

            BookingAnalysisModel analysisModel = new BookingAnalysisModel()
            {
                TotalBookings = bookings.Count(),
                TotalConfirmedBookings = bookings.Count(b => b.Status == BookingStatus.CONFIRMED),
                TotalCompletedBookings = bookings.Count(b => b.Status == BookingStatus.COMPLETED),
                TotalCanceledBookings = bookings.Count(b => b.Status == BookingStatus.CANCELED_BY_BOOKER)
            };

            return analysisModel;
        }

        #region Private members
        //private async Task<bool> IsEnoughWalletBalanceToBook(
        //    BookingCreateModel newBooking, CancellationToken cancellationToken)
        //{
        //    // Get uncompleted Booking Details
        //    IEnumerable<Booking> bookings = await work.Bookings
        //        .GetAllAsync(query => query.Where(b => b.CustomerId.Equals(newBooking.CustomerId.Value)),
        //        cancellationToken: cancellationToken);

        //    IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);
        //    IEnumerable<BookingDetail> futureBookingDetails = await work.BookingDetails
        //        .GetAllAsync(query => query.Where(
        //            d => bookingIds.Contains(d.BookingId) &&
        //            d.Status != BookingDetailStatus.COMPLETED
        //            && d.Status != BookingDetailStatus.CANCELLED),
        //            cancellationToken: cancellationToken);

        //    // Get Wallet
        //    Wallet wallet = await work.Wallets.GetAsync(w => w.UserId.Equals(newBooking.CustomerId.Value),
        //        cancellationToken: cancellationToken);
        //    double totalPrice = newBooking.PriceAfterDiscount +
        //        futureBookingDetails.Sum(d => d.PriceAfterDiscount.Value);

        //    return wallet.Balance >= totalPrice;
        //}
        private void IsValidRoundTripRoutines(IList<RouteRoutineListItemModel> routines,
            IList<RouteRoutineListItemModel> roundTripRoutines)
        {
            if (!routines.Any() || !roundTripRoutines.Any())
            {
                throw new ApplicationException("Dữ liệu lịch trình không hợp lệ!!");
            }

            if (roundTripRoutines.Count() != routines.Count)
            {
                throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
                    $"Tuyến đường chiều về có tổng cộng {roundTripRoutines.Count()} lịch trình, trong khi lịch trình " +
                    $"cho tuyến đường chiều đi sắp được thiết lập có {routines.Count} lịch trình!");
            }

            foreach (RouteRoutineListItemModel roundTripRoutine in roundTripRoutines)
            {
                DateOnly routineDate = roundTripRoutine.RoutineDate;
                var routine = routines.SingleOrDefault(r => r.RoutineDate.Equals(routineDate
                    ));

                if (routine is null)
                {
                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
                        $"Tuyến đường chiều về có lịch trình cho ngày {routineDate} nhưng không tìm thấy " +
                        $"lịch trình cho tuyến đường chiều đi cho ngày này!!");
                }
                DateTime pickupDateTime = routineDate.ToDateTime(routine.PickupTime);
                DateTime roundTripPickupDateTime = routineDate
                    .ToDateTime(roundTripRoutine.PickupTime).AddMinutes(30);

                if (pickupDateTime > roundTripPickupDateTime)
                {
                    // Newly setup pickup time is later than the roundtrip pickup + 30 minutes
                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
                        $"Tuyến đường chiều về có lịch trình cho ngày {routineDate} vào lúc " +
                        $"{roundTripRoutine.PickupTime} nhưng lịch trình cho tuyến đường chiều đi cho ngày này " +
                        $"lại được xếp trễ hơn quá 30 phút ({routine.PickupTime})!!");
                }
            }
        }

        private async Task<IEnumerable<Booking>> FilterBookings(IEnumerable<Booking> bookings,
            BookingFilterParameters filters,
             CancellationToken cancellationToken)
        {
            bookings = bookings.Where(
                b =>
                {
                    return
                    (!filters.MinStartDate.HasValue || DateOnly.FromDateTime(b.StartDate) >= filters.MinStartDate.Value)
                    && (!filters.MaxStartDate.HasValue || DateOnly.FromDateTime(b.StartDate) <= filters.MaxStartDate.Value)
                    && (!filters.MinEndDate.HasValue || DateOnly.FromDateTime(b.EndDate) >= filters.MinEndDate.Value)
                    && (!filters.MaxEndDate.HasValue || DateOnly.FromDateTime(b.EndDate) >= filters.MaxEndDate.Value);
                });

            return bookings;
        }

        private async Task<(int, int, int)> CountBookingDetailsAsync(Guid bookingId,
            CancellationToken cancellationToken)
        {
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    d => d.BookingId.Equals(bookingId)), cancellationToken: cancellationToken);
            if (!bookingDetails.Any())
            {
                return (0, 0, 0);
            }
            int bookingDetailsCount = bookingDetails.Count();
            int assignedBookingDetailsCount = bookingDetails.Count(d => d.DriverId.HasValue);
            int completedBookingDetailsCount = bookingDetails.Count(d => d.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF
                || d.Status == BookingDetailStatus.COMPLETED);
            return (bookingDetailsCount, assignedBookingDetailsCount, completedBookingDetailsCount);
        }
        #endregion
    }
}
