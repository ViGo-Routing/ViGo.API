using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Station startStation = await work.Stations.GetAsync(bookingDetail.StartStationId, cancellationToken: cancellationToken);
            Station endStation = await work.Stations.GetAsync(bookingDetail.EndStationId, cancellationToken: cancellationToken);

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
            User driver = await work.Users.GetAsync(driverId, cancellationToken: cancellationToken);
            if (driver == null)
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
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

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
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

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
    }
}
