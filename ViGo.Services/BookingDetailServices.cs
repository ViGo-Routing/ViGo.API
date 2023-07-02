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
using ViGo.Models.RouteRoutines;
using ViGo.Models.Routes;
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Google;

namespace ViGo.Services
{
    public class BookingDetailServices : BaseServices
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
            if (driver == null || driver.Role == UserRole.DRIVER)
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
            switch (updateDto.Status)
            {
                case BookingDetailStatus.ARRIVE_AT_PICKUP:
                    bookingDetail.ArriveAtPickupTime = updateDto.Time;
                    break;
                case BookingDetailStatus.GOING:
                    bookingDetail.PickupTime = updateDto.Time;
                    break;
                case BookingDetailStatus.ARRIVE_AT_DROPOFF:
                    bookingDetail.DropoffTime = updateDto.Time;
                    break;
            }

            await work.BookingDetails.UpdateAsync(bookingDetail);

            if (updateDto.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
            {
                // Calculate driver wage
                // Withdraw from System Wallet to pay for Driver
                FareServices fareServices = new FareServices(work, _logger);
                if (!bookingDetail.PriceAfterDiscount.HasValue)
                {
                    throw new ApplicationException("Chuyến đi thiếu thông tin dữ liệu!!");
                }

                double driverWage = await fareServices.CalculateDriverWage(
                    bookingDetail.PriceAfterDiscount.Value, cancellationToken);

                // Get SYSTEM WALLET
                Wallet systemWallet = await work.Wallets.GetAsync(w =>
                    w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);
                if (systemWallet is null)
                {
                    throw new Exception("Chưa có ví dành cho hệ thống!!");
                }

                //WalletTransaction systemTransaction_Withdraw = new WalletTransaction
                //{
                //    WalletId = systemWallet.Id,
                //    Amount = driverWage,
                //    BookingDetailId = bookingDetail.Id,
                //    BookingId = bookingDetail.BookingId,
                //    Type = WalletTransactionType.PAY_FOR_DRIVER,
                //    Status = WalletTransactionStatus.SUCCESSFULL,
                //};

                //Wallet driverWallet = await work.Wallets.GetAsync(w =>
                //    w.UserId.Equals(bookingDetail.DriverId.Value), cancellationToken: cancellationToken);
                //if (driverWallet is null)
                //{
                //    throw new ApplicationException("Tài xế chưa được cấu hình ví!!");
                //}

                //WalletTransaction driverTransaction_Add = new WalletTransaction
                //{
                //    WalletId = driverWallet.Id,
                //    Amount = driverWage,
                //    BookingDetailId = bookingDetail.Id,
                //    BookingId = bookingDetail.BookingId,
                //    Type = WalletTransactionType.TRIP_INCOME,
                //    Status = WalletTransactionStatus.SUCCESSFULL
                //};

                //systemWallet.Balance -= driverWage;
                //driverWallet.Balance += driverWage;

                //await work.WalletTransactions.InsertAsync(systemTransaction_Withdraw, cancellationToken: cancellationToken);
                //await work.WalletTransactions.InsertAsync(driverTransaction_Add, cancellationToken: cancellationToken);
                
                //await work.Wallets.UpdateAsync(systemWallet);
                //await work.Wallets.UpdateAsync(driverWallet);
            }

            await work.SaveChangesAsync(cancellationToken);

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

            bookingDetail.DriverId = dto.DriverId;
            bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
            bookingDetail.Status = BookingDetailStatus.ASSIGNED;

            await work.BookingDetails.UpdateAsync(bookingDetail);
            await work.SaveChangesAsync(cancellationToken);

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
                } else
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
                    } else
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
                                Longtitude = startStation.Longtitude
                            },
                            EndLocation = new GoogleMapPoint
                            {
                                Latitude = endStation.Latitude,
                                Longtitude = endStation.Longtitude
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
                                    AddToPrioritizedBookingDetails(prioritizedBookingDetails, bookingDetail,
                                        addedTripAsNode.Value, previousTrip, nextTrip);
                                }
                            }
                            // Previous and Next cannot be both null
                        } else
                        {
                            // Has Previous trip
                            if (addedTripAsNode.Value.BeginTime > previousTrip.EndTime)
                            {
                                if (nextTrip != null)
                                {
                                    if (addedTripAsNode.Value.EndTime < nextTrip.BeginTime)
                                    {
                                        // Valid one
                                        AddToPrioritizedBookingDetails(prioritizedBookingDetails, bookingDetail,
                                            addedTripAsNode.Value, previousTrip, nextTrip);
                                    }
                                } else
                                {
                                    // Valid one
                                    AddToPrioritizedBookingDetails(prioritizedBookingDetails, bookingDetail,
                                        addedTripAsNode.Value, previousTrip, nextTrip);
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

            IList<DriverTripsOfDate> driverTrips = await GetDriverSchedulesAsync(driverId, cancellationToken);

            if (driverTrips.Count == 0)
            {
                // Has no trips
            } else
            {
                // Has Trips
                DriverTripsOfDate? tripsOfDate = driverTrips.SingleOrDefault(
                    t => t.Date == DateOnly.FromDateTime(bookingDetail.Date));
                if (tripsOfDate is null || tripsOfDate.Trips.Count == 0)
                {
                    // No trips in day

                } else
                {
                    // Has trips in day
                    TimeSpan bookingDetailEndTime = DateTimeUtilities.CalculateTripEndTime(
                        bookingDetail.CustomerDesiredPickupTime, booking.Duration);

                    Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId, includeDeleted: true, 
                        cancellationToken: cancellationToken);
                    Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId, includeDeleted: true, 
                        cancellationToken: cancellationToken);

                    DriverTrip addedTrip = new DriverTrip
                    {
                        Id = bookingDetail.Id,
                        BeginTime = bookingDetail.CustomerDesiredPickupTime,
                        EndTime = bookingDetailEndTime,
                        StartLocation = new GoogleMapPoint
                        {
                            Latitude = startStation.Latitude,
                            Longtitude = startStation.Longtitude
                        },
                        EndLocation = new GoogleMapPoint
                        {
                            Latitude = endStation.Latitude,
                            Longtitude = endStation.Longtitude
                        }
                    };

                    IEnumerable<DriverTrip> addedTrips = tripsOfDate.Trips.Append(addedTrip).OrderBy(t => t.BeginTime);

                    LinkedList<DriverTrip> addedTripsAsLinkedList = new LinkedList<DriverTrip>(addedTrips);
                    LinkedListNode<DriverTrip> addedTripAsNode = addedTripsAsLinkedList.Find(addedTrip);

                    DriverTrip? previousTrip = addedTripAsNode.Previous?.Value;
                    DriverTrip? nextTrip = addedTripAsNode.Next?.Value;

                    if (previousTrip != null)
                    {
                        if (addedTripAsNode.Value.BeginTime <= previousTrip.EndTime)
                        {
                            // Invalid one
                            throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                $"Bạn đang chọn chuyến đi có thời gian bắt đầu ({addedTripAsNode.Value.BeginTime}) " +
                                $"sớm hơn so với thời gian dự kiến bạn sẽ kết thúc một chuyến đi bạn đã chọn trước đó ({previousTrip.EndTime})");
                        }
                    }

                    if (nextTrip != null)
                    {
                        // Has Next trip
                        if (addedTripAsNode.Value.EndTime >= nextTrip.BeginTime)
                        {
                            // Invalid one
                            throw new ApplicationException($"Thời gian của chuyến đi bạn chọn không phù hợp với lịch trình của bạn! \n" +
                                $"Bạn đang chọn chuyến đi có thời gian kết thúc dự kiến ({addedTripAsNode.Value.EndTime}) " +
                                $"trễ hơn so với thời gian bạn phải bắt đầu một chuyến đi bạn đã chọn trước đó ({nextTrip.BeginTime})");
                        }
                    }
                }
            }

            bookingDetail.DriverId = driverId;
            bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
            bookingDetail.Status = BookingDetailStatus.ASSIGNED;

            await work.BookingDetails.UpdateAsync(bookingDetail);
            await work.SaveChangesAsync(cancellationToken);

            return bookingDetail;
        }

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
                                                        StartLocation = new Utilities.Google.GoogleMapPoint()
                                                        {
                                                            Latitude = startStation.Latitude,
                                                            Longtitude = startStation.Longtitude
                                                        },
                                                        EndLocation = new Utilities.Google.GoogleMapPoint()
                                                        {
                                                            Latitude = endStation.Latitude,
                                                            Longtitude = endStation.Longtitude
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


        private async void AddToPrioritizedBookingDetails(IList<DriverMappingItem> prioritizedBookingDetails,
            BookingDetail bookingDetailToAdd, DriverTrip addedTrip,
            DriverTrip? previousTrip, DriverTrip? nextTrip)
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
                        addedTrip.EndLocation, nextTrip.StartLocation);

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
                        previousTrip.EndLocation, addedTrip.StartLocation);

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
                        previousTrip.EndLocation, addedTrip.StartLocation);
                    int movingDurationAddedToNext = await GoogleMapsApiUtilities.GetDistanceBetweenTwoPointsAsync(
                        addedTrip.EndLocation, nextTrip.StartLocation);

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
    }
}
