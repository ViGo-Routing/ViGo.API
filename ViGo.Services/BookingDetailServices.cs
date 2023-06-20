using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.BookingDetails;
using ViGo.Models.Routes;
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;

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

            IEnumerable<Guid> stationIds = new List<Guid>();

            Route? driverRoute = null;
            if (bookingDetail.DriverRouteId.HasValue)
            {
                driverRoute = await work.Routes
                    .GetAsync(bookingDetail.DriverRouteId.Value, cancellationToken: cancellationToken);
                if (driverRoute.StartStationId.HasValue && driverRoute.EndStationId.HasValue)
                {
                    stationIds = stationIds.Append(driverRoute.StartStationId.Value)
                        .Append(driverRoute.EndStationId.Value);
                }

            }

            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

            //RouteViewModel customerRouteDto = new RouteViewModel(
            //    customerRoute,
            //    new StationViewModel(
            //        stations.SingleOrDefault(s => s.Id.Equals(customerRoute.StartStationId)), 1),
            //    new StationViewModel(
            //        stations.SingleOrDefault(s => s.Id.Equals(customerRoute.EndStationId)), 2));

            RouteViewModel? driverRouteDto = null;
            if (driverRoute != null)
            {
                driverRouteDto = new RouteViewModel(
                driverRoute,
                driverRoute.StartStationId.HasValue ?
                new StationViewModel(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.StartStationId.Value)), 1) : null,
                driverRoute.EndStationId.HasValue ?
                new StationViewModel(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.EndStationId)), 2) : null) ;
            }

            BookingDetailViewModel dto = new BookingDetailViewModel(
                bookingDetail, driverDto, /*customerRouteDto,*/ driverRouteDto);
            //BookingDetailViewModel dto = new BookingDetailViewModel(bookingDetail);

            return dto;
        
        }

        public async Task<IEnumerable<BookingDetailViewModel>>
            GetDriverAssignedBookingDetailsAsync(Guid driverId, CancellationToken cancellationToken)
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
            if (!bookingDetails.Any())
            {
                return new List<BookingDetailViewModel>();
            }

            IEnumerable<Guid> routeIds = (bookingDetails.Where(bd => bd.DriverRouteId.HasValue)
                .Select(bd =>
                bd.DriverRouteId.Value))
                .Distinct();

            IEnumerable<Route> routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => routeIds.Contains(r.Id)), cancellationToken: cancellationToken);

            IEnumerable<Guid> stationIds = routes.Where(r => r.StartStationId.HasValue).Select(
                r => r.StartStationId.Value).Concat(routes.Where(r => r.EndStationId.HasValue).Select(
                    r => r.EndStationId.Value)).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);


            IEnumerable<BookingDetailViewModel> dtos =
                from bookingDetail in bookingDetails
                //join customerRoute in routes
                //    on bookingDetail.CustomerRouteId equals customerRoute.Id
                //join customerStartStation in stations
                //    on customerRoute.StartStationId equals customerStartStation.Id
                //join customerEndStation in stations
                //    on customerRoute.EndStationId equals customerEndStation.Id
                join driverRoute in routes
                    on bookingDetail.DriverRouteId equals driverRoute.Id
                join driverStartStation in stations
                    on driverRoute.StartStationId equals driverStartStation.Id
                join driverEndStation in stations
                    on driverRoute.EndStationId equals driverEndStation.Id
                select new BookingDetailViewModel(
                    bookingDetail, null,
                    //new RouteViewModel(customerRoute,
                    //    new StationViewModel(customerStartStation, 1),
                    //    new StationViewModel(customerEndStation, 2)),
                    new RouteViewModel(driverRoute,
                        new StationViewModel(driverStartStation, 1),
                        new StationViewModel(driverEndStation, 2)));
            //IEnumerable<BookingDetailViewModel> dtos =
            //    from bookingDetail in bookingDetails
            //    select new BookingDetailViewModel(bookingDetail);

            return dtos;
        }

        public async Task<IEnumerable<BookingDetailViewModel>>
            GetBookingDetailsAsync(Guid bookingId, CancellationToken cancellationToken)
        {
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    d => d.BookingId.Equals(bookingId)), cancellationToken: cancellationToken);

            IEnumerable<Guid> driverIds = bookingDetails.Where(
                d => d.DriverId.HasValue).Select(d => d.DriverId.Value);
            IEnumerable<User> drivers = await work.Users
                .GetAllAsync(query => query.Where(
                    u => driverIds.Contains(u.Id)), cancellationToken: cancellationToken);

            IEnumerable<BookingDetailViewModel> dtos =
                new List<BookingDetailViewModel>();
            foreach (BookingDetail bookingDetail in bookingDetails)
            {
                if (bookingDetail.DriverId.HasValue)
                {
                    User driver = drivers.SingleOrDefault(u => u.Id.Equals(bookingDetail.DriverId));
                    dtos = dtos.Append(new BookingDetailViewModel(bookingDetail, new UserViewModel(driver)));
                } else
                {
                    dtos = dtos.Append(new BookingDetailViewModel(bookingDetail, null));
                }
            }

            return dtos;
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
            await work.SaveChangesAsync(cancellationToken);

            return bookingDetail;
        }

        public async Task<BookingDetail> AssignDriverAsync(BookingDetailAssignDriverModel dto, CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(dto.BookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail == null)
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
    }
}
