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
    public class BookingDetailServices : BaseServices<BookingDetail>
    {
        public BookingDetailServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<BookingDetailViewModel?> GetBookingDetailAsync(
            Guid bookingDetailId)
        {
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(bookingDetailId);
            if (bookingDetail == null)
            {
                return null;
            }

            UserViewModel? driverDto = null;
            if (bookingDetail.DriverId.HasValue)
            {
                User driver = await work.Users.GetAsync(bookingDetail.DriverId.Value);
                driverDto = new UserViewModel(driver);
            }

            Route customerRoute = await work.Routes.GetAsync(bookingDetail.CustomerRouteId);

            IEnumerable<Guid> stationIds = (new List<Guid>
            {
                customerRoute.StartStationId,
                customerRoute.EndStationId,

            }).Distinct();

            Route? driverRoute = null;
            if (bookingDetail.DriverRouteId.HasValue)
            {
                driverRoute = await work.Routes
                    .GetAsync(bookingDetail.DriverRouteId.Value);
                stationIds = stationIds.Append(driverRoute.StartStationId)
                    .Append(driverRoute.EndStationId);
                
            }

            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));

            RouteViewModel customerRouteDto = new RouteViewModel(
                customerRoute,
                new StationViewModel(
                    stations.SingleOrDefault(s => s.Id.Equals(customerRoute.StartStationId)), 1),
                new StationViewModel(
                    stations.SingleOrDefault(s => s.Id.Equals(customerRoute.EndStationId)), 2));

            RouteViewModel? driverRouteDto = null;
            if (driverRoute != null)
            {
                driverRouteDto = new RouteViewModel(
                driverRoute,
                new StationViewModel(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.StartStationId)), 1),
                new StationViewModel(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.EndStationId)), 2));
            }

            BookingDetailViewModel dto = new BookingDetailViewModel(
                bookingDetail, driverDto, customerRouteDto, driverRouteDto);

            return dto;
        
        }

        public async Task<IEnumerable<BookingDetailViewModel>>
            GetDriverAssignedBookingDetailsAsync(Guid driverId)
        {
            User driver = await work.Users.GetAsync(driverId);
            if (driver == null)
            {
                throw new ApplicationException("Tài xế không tồn tại!!!");
            }

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.DriverId.HasValue &&
                    bd.DriverId.Value.Equals(driverId)));
            if (!bookingDetails.Any())
            {
                return new List<BookingDetailViewModel>();
            }

            IEnumerable<Guid> routeIds = bookingDetails
                .Select(bd => bd.CustomerRouteId)
                .Concat(bookingDetails.Where(bd => bd.DriverRouteId.HasValue)
                .Select(bd => 
                bd.DriverRouteId.Value))
                .Distinct();

            IEnumerable<Route> routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => routeIds.Contains(r.Id)));

            IEnumerable<Guid> stationIds = routes.Select(
                r => r.StartStationId).Concat(routes.Select(
                    r => r.EndStationId)).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));


            IEnumerable<BookingDetailViewModel> dtos =
                from bookingDetail in bookingDetails
                join customerRoute in routes
                    on bookingDetail.CustomerRouteId equals customerRoute.Id
                join customerStartStation in stations
                    on customerRoute.StartStationId equals customerStartStation.Id
                join customerEndStation in stations
                    on customerRoute.EndStationId equals customerEndStation.Id
                join driverRoute in routes
                    on bookingDetail.DriverRouteId equals driverRoute.Id
                join driverStartStation in stations
                    on customerRoute.StartStationId equals driverStartStation.Id
                join driverEndStation in stations
                    on customerRoute.EndStationId equals driverEndStation.Id
                select new BookingDetailViewModel(
                    bookingDetail, null,
                    new RouteViewModel(customerRoute,
                        new StationViewModel(customerStartStation, 1),
                        new StationViewModel(customerEndStation, 2)),
                    new RouteViewModel(driverRoute,
                        new StationViewModel(driverStartStation, 1),
                        new StationViewModel(driverEndStation, 2)));

            return dtos;
        }

        public async Task<BookingDetail> UpdateBookingDetailStatusAsync(
            BookingDetailUpdateStatusModel updateDto)
        {
            if (!Enum.IsDefined(updateDto.Status))
            {
                throw new ApplicationException("Trạng thái Booking Detail không hợp lệ!");
            }

            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(updateDto.BookingDetailId);
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
            await work.SaveChangesAsync();

            return bookingDetail;
        }

        public async Task<BookingDetail> AssignDriverAsync(BookingDetailAssignDriverModel dto)
        {
            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(dto.BookingDetailId);
            if (bookingDetail == null)
            {
                throw new ApplicationException("Booking Detail không tồn tại!!");
            }

            User driver = await work.Users
                .GetAsync(dto.DriverId);
            if (driver == null || driver.Role != UserRole.DRIVER)
            {
                throw new ApplicationException("Tài xế không tồn tại!!");
            }

            bookingDetail.DriverId = dto.DriverId;
            bookingDetail.AssignedTime = DateTimeUtilities.GetDateTimeVnNow();
            bookingDetail.Status = BookingDetailStatus.ASSIGNED;

            await work.BookingDetails.UpdateAsync(bookingDetail);
            await work.SaveChangesAsync();

            return bookingDetail;
        }
    }
}
