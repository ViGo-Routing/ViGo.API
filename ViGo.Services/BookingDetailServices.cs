using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.BookingDetails;
using ViGo.DTOs.Routes;
using ViGo.DTOs.Stations;
using ViGo.DTOs.Users;
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

        public async Task<BookingDetailListItemDto?> GetBookingDetailAsync(
            Guid bookingDetailId)
        {
            BookingDetail bookingDetail = await work.BookingDetails.GetAsync(bookingDetailId);
            if (bookingDetail == null)
            {
                return null;
            }

            UserListItemDto? driverDto = null;
            if (bookingDetail.DriverId.HasValue)
            {
                User driver = await work.Users.GetAsync(bookingDetail.DriverId.Value);
                driverDto = new UserListItemDto(driver);
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

            RouteListItemDto customerRouteDto = new RouteListItemDto(
                customerRoute,
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(customerRoute.StartStationId)), 1),
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(customerRoute.EndStationId)), 2));

            RouteListItemDto? driverRouteDto = null;
            if (driverRoute != null)
            {
                driverRouteDto = new RouteListItemDto(
                driverRoute,
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.StartStationId)), 1),
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.EndStationId)), 2));
            }

            BookingDetailListItemDto dto = new BookingDetailListItemDto(
                bookingDetail, driverDto, customerRouteDto, driverRouteDto);

            return dto;
        
        }

        public async Task<IEnumerable<BookingDetailListItemDto>>
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
                return new List<BookingDetailListItemDto>();
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


            IEnumerable<BookingDetailListItemDto> dtos =
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
                select new BookingDetailListItemDto(
                    bookingDetail, null,
                    new RouteListItemDto(customerRoute,
                        new StationListItemDto(customerStartStation, 1),
                        new StationListItemDto(customerEndStation, 2)),
                    new RouteListItemDto(driverRoute,
                        new StationListItemDto(driverStartStation, 1),
                        new StationListItemDto(driverEndStation, 2)));

            return dtos;
        }

        public async Task<BookingDetail> UpdateBookingDetailStatusAsync(
            BookingDetailUpdateStatusDto updateDto)
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

        public async Task<BookingDetail> AssignDriverAsync(BookingDetailAssignDriverDto dto)
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
