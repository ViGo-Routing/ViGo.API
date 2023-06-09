﻿using Microsoft.AspNetCore.Http;
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
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
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

        public async Task<IPagedEnumerable<BookingViewModel>>
            GetBookingsAsync(Guid? userId,
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Booking> bookings =
                await work.Bookings.GetAllAsync(
                    query => query.Where(
                        b =>
                        (userId != null && userId.HasValue) ?
                        b.CustomerId.Equals(userId.Value)
                        : true), cancellationToken: cancellationToken);

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

            BookingViewModel dto = new BookingViewModel(booking,
                customerDto, route, 
                new StationViewModel(startStation),
                new StationViewModel(endStation), vehicleType);
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
            } else
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
                } else
                {
                    roundTrip = await work.Routes.GetAsync(route.RoundTripRouteId.Value,
                        cancellationToken: cancellationToken);
                }
            }

            VehicleType vehicleType = await work.VehicleTypes
                .GetAsync(model.VehicleTypeId, cancellationToken: cancellationToken);
            if (vehicleType is null)
            {
                throw new ApplicationException("Loại phương tiện di chuyển không hợp lệ!!");
            }

            Promotion? promotion = null;
            if (model.PromotionId.HasValue)
            {
                promotion = await work.Promotions.GetAsync(model.PromotionId.Value,
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
                if (promotion.MaxTotalUsage.HasValue)
                {
                    if (promotion.TotalUsage == promotion.MaxTotalUsage)
                    {
                        throw new ApplicationException("Mã khuyến mãi đã vượt quá số lượt sử dụng!!");
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

            if (!routeRoutines.Any())
            {
                throw new ApplicationException("Không có lịch trình nào thích hợp với thời gian " +
                    "của chuyến đi đã được thiết lập!!");
            }
            if (roundTrip != null)
            {
                IEnumerable<RouteRoutine> roundTripRoutines = await work.RouteRoutines.GetAllAsync(
                    query => query.Where(r => r.RouteId.Equals(roundTrip.Id)
                    && r.RoutineDate >= model.StartDate
                    && r.RoutineDate <= model.EndDate), cancellationToken: cancellationToken);

                if (!roundTripRoutines.Any())
                {
                    throw new ApplicationException("Không có lịch trình nào của chuyến về thích hợp với thời gian " +
                    "của chuyến đi đã được thiết lập!!");
                }
                routeRoutines = routeRoutines.Concat(roundTripRoutines);
            }

            int bookingDetailCount = routeRoutines.Count();

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

            // All is valid
            Booking booking = new Booking()
            {
                CustomerId = model.CustomerId.Value,
                CustomerRouteId = model.CustomerRouteId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                TotalPrice = model.TotalPrice,
                PriceAfterDiscount = model.PriceAfterDiscount,
                IsShared = model.IsShared,
                Duration = model.Duration,
                Distance = model.Distance,
                PromotionId = model.PromotionId,
                VehicleTypeId = model.VehicleTypeId,
                //Type = model.Type,
                Status = BookingStatus.DRAFT // TODO Code
            };

            Wallet customerWallet = await work.Wallets.GetAsync(
                w => w.UserId.Equals(model.CustomerId), cancellationToken: cancellationToken);
            // TODO Code
            if (customerWallet.Balance >= model.PriceAfterDiscount)
            {
                booking.Status = BookingStatus.CONFIRMED;
            }

            await work.Bookings.InsertAsync(booking,
                cancellationToken: cancellationToken);

            // Generate BookingDetails

            double priceEachTrip = booking.TotalPrice.Value / bookingDetailCount;
            double priceAfterDiscountEachTrip = booking.PriceAfterDiscount.Value / bookingDetailCount;

            FareServices fareServices = new FareServices(work, _logger);

            foreach (RouteRoutine routeRoutine in routeRoutines)
            {
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
                    Status = BookingDetailStatus.PENDING_ASSIGN
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

            if (promotion != null)
            {
                promotion.TotalUsage += 1;
                await work.Promotions.UpdateAsync(promotion);
            }

            await work.SaveChangesAsync(cancellationToken);
            return booking;
        }
    }
}
