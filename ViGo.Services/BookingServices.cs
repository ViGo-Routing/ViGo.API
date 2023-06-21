using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.Bookings;
using ViGo.Models.RouteStations;
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class BookingServices : BaseServices
    {
        public BookingServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IEnumerable<BookingViewModel>>
            GetBookingsAsync(Guid? userId, CancellationToken cancellationToken)
        {
            IEnumerable<Booking> bookings =
                await work.Bookings.GetAllAsync(
                    query => query.Where(
                        b =>
                        (userId != null && userId.HasValue) ?
                        b.CustomerId.Equals(userId.Value)
                        : true), cancellationToken: cancellationToken);

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
                from user in users
                select new UserViewModel(user);

            //IEnumerable<Guid> customerRouteIds = bookings.Select(b => b.CustomerRouteId).Distinct();
            //IEnumerable<Route> customerRoutes = await work.Routes.GetAllAsync(
            //    query => query.Where(
            //        r => customerRouteIds.Contains(r.Id)), cancellationToken: cancellationToken);

            ////IEnumerable<RouteStation> routeStations = await work.RouteStations
            ////    .GetAllAsync(query => query.Where(
            ////        rs => routeStationIds.Contains(rs.Id)), cancellationToken: cancellationToken);
            //IEnumerable<Guid> stationIds =
            //    customerRoutes.Select(rs => rs.StartStationId)
            //    .Concat(customerRoutes.Select(r => r.EndStationId))
            //    .Distinct();
            //IEnumerable<Station> stations = await work.Stations
            //    .GetAllAsync(query => query.Where(
            //        s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> vehicleTypeIds = bookings.Select(b => b.VehicleTypeId).Distinct();
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes
                .GetAllAsync(query => query.Where(
                    v => vehicleTypeIds.Contains(v.Id)), cancellationToken: cancellationToken);

            IEnumerable<BookingViewModel> dtos =
                from booking in bookings
                join customer in userDtos
                    on booking.CustomerId equals customer.Id
                //join route in customerRoutes
                //    on booking.CustomerRouteId equals route.Id
                //join startRouteStation in routeStations
                //    on booking.StartRouteStationId equals startRouteStation.Id
                //join startStation in stations
                //    on route.StartStationId equals startStation.Id
                //join endRouteStation in routeStations
                //    on booking.EndRouteStationId equals endRouteStation.Id
                //join endStation in stations
                //    on route.EndStationId equals endStation.Id
                join vehicleType in vehicleTypes
                    on booking.VehicleTypeId equals vehicleType.Id
                select new BookingViewModel(
                    booking, customer, /*route,*/
                    //new StationViewModel(startStation, 1),
                    //new StationViewModel(endStation, 2),
                    vehicleType
                    );
            //IEnumerable<BookingViewModel> dtos
            //    = from booking in bookings
            //      select new BookingViewModel(booking);

            return dtos;
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
            Station? startStation = null;
            if (route.StartStationId.HasValue)
            {
                startStation = await work.Stations.GetAsync(route.StartStationId.Value,
                    cancellationToken: cancellationToken);
            }
            Station? endStation = null;
            if (route.EndStationId.HasValue)
            {
                endStation = await work.Stations.GetAsync(route.EndStationId.Value,
                    cancellationToken: cancellationToken);
            }

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

            BookingViewModel dto = new BookingViewModel(booking,
                customerDto, route, 
                new StationViewModel(startStation, 1),
                new StationViewModel(endStation, 2), vehicleType);
            //BookingViewModel dto = new BookingViewModel(booking);

            return dto;
        }

        public async Task<Booking> CreateBookingAsync(
            BookingCreateModel model, CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(model.PaymentMethod))
            {
                throw new ApplicationException("Phương thức thanh toán không hợp lệ!!");
            }
            if (model.Distance <= 0)
            {
                throw new ApplicationException("Khoảng cách phải lớn hơn 0!");
            }
            if (model.Duration <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển phải lớn hơn 0!");
            }

            User user = await work.Users.GetAsync(model.CustomerId,
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

            VehicleType vehicleType = await work.VehicleTypes
                .GetAsync(model.VehicleTypeId, cancellationToken: cancellationToken);
            if (vehicleType is null)
            {
                throw new ApplicationException("Loại phương tiện di chuyển không hợp lệ!!");
            }

            if (model.PromotionId.HasValue)
            {
                Promotion promotion = await work.Promotions.GetAsync(model.PromotionId.Value,
                    cancellationToken: cancellationToken);
                if (promotion is null ||
                    promotion.Status == PromotionStatus.UNAVAILABLE ||
                    (promotion.ExpireTime.HasValue && promotion.ExpireTime < DateTimeUtilities.GetDateTimeVnNow())
                    || (promotion.StartTime > DateTimeUtilities.GetDateTimeVnNow())
                    )
                {
                    throw new ApplicationException("Thông tin khuyến mãi không hợp lệ hoặc đã hết hạn sử dụng!");
                }

                if (!promotion.VehicleTypeId.Equals(vehicleType.Id))
                {
                    throw new ApplicationException("Loại phương tiện và thông tin khuyến mãi không hợp lệ!!");
                }
                if (promotion.MinTotalPrice.HasValue &&
                    promotion.MinTotalPrice < model.TotalPrice)
                {
                    throw new ApplicationException("Booking chuyến đi chưa đạt giá trị tối thiểu để áp dụng khuyến mãi!!");
                }

                // Check Max Usage per user
                if (promotion.UsagePerUser.HasValue)
                {
                    int usageCount = (await work.Bookings
                        .GetAllAsync(query => query.Where(
                            b => b.CustomerId.Equals(user.Id)
                            && b.PromotionId.Equals(promotion.Id)),
                            cancellationToken: cancellationToken)).Count();
                    if (usageCount > promotion.UsagePerUser.Value)
                    {
                        throw new ApplicationException("Mã khuyến mãi đã vượt quá số lần sử dụng!!");
                    }
                }
            }

            //TODO Code
            // Check Start Date and End Date
            //IEnumerable<RouteRoutine> routeRoutines = await
            //    work.RouteRoutines.GetAllAsync(query => query.Where(
            //        r => r.RouteId.Equals(route.Id)), cancellationToken: cancellationToken);

            DateTimeRange newRange = new DateTimeRange(
                model.StartDate, model.EndDate);

            IEnumerable<Booking> currentBookings = await
                work.Bookings.GetAllAsync(query => query.Where(
                    b => b.CustomerRouteId.Equals(model.CustomerRouteId)),
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

            int bookingDetailCount = routeRoutines.Count();

            if (bookingDetailCount == 0)
            {
                throw new ApplicationException("Không có lịch trình nào thích hợp với thời gian " +
                    "của chuyến đi đã được thiết lập!!");
            }

            // All is valid
            Booking booking = new Booking()
            {
                CustomerId = model.CustomerId,
                CustomerRouteId = model.CustomerRouteId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                StartTime = routeRoutines.First().StartTime,
                TotalPrice = model.TotalPrice,
                PriceAfterDiscount = model.PriceAfterDiscount,
                PaymentMethod = model.PaymentMethod,
                IsShared = model.IsShared,
                Duration = model.Duration,
                Distance = model.Distance,
                PromotionId = model.PromotionId,
                VehicleTypeId = model.VehicleTypeId,
                Status = BookingStatus.UNPAID // TODO Code
            };
            await work.Bookings.InsertAsync(booking,
                cancellationToken: cancellationToken);

            // Generate BookingDetails

            double priceEachTrip = booking.TotalPrice.Value / bookingDetailCount;
            double priceAfterDiscountEachTrip = booking.PriceAfterDiscount.Value / bookingDetailCount;

            foreach (RouteRoutine routeRoutine in routeRoutines)
            {
                BookingDetail bookingDetail = new BookingDetail
                {
                    BookingId = booking.Id,
                    Date = routeRoutine.RoutineDate.Value,
                    Price = priceEachTrip,
                    PriceAfterDiscount = priceAfterDiscountEachTrip,
                    DriverWage = FareUtilities.DriverWagePercent * priceEachTrip,
                    BeginTime = routeRoutine.StartTime.Value,
                    Status = BookingDetailStatus.PENDING
                };
                await work.BookingDetails.InsertAsync(bookingDetail,
                    cancellationToken: cancellationToken);
            }

            await work.SaveChangesAsync(cancellationToken);
            return booking;
        }
    }
}
