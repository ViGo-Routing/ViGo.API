using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.DTOs.Bookings;
using ViGo.DTOs.RouteStations;
using ViGo.DTOs.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class BookingServices : BaseServices<Booking>
    {
        public BookingServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<IEnumerable<BookingListItemDto>>
            GetBookingsAsync(Guid? userId = null)
        {
            IEnumerable<Booking> bookings =
                await work.Bookings.GetAllAsync(
                    query => query.Where(
                        b =>
                        (userId != null && userId.HasValue) ?
                        b.CustomerId.Equals(userId.Value)
                        : true));

            IEnumerable<Guid> customerIds = bookings.Select(b => b.CustomerId);
            IEnumerable<Guid> routeStationIds = bookings.Select(
                b => b.StartRouteStationId).Concat(bookings.Select(
                    b => b.EndRouteStationId));

            IEnumerable<User> users = await work.Users
                .GetAllAsync(query => query.Where(
                    u => customerIds.Contains(u.Id)));
            IEnumerable<UserListItemDto> userDtos =
                from user in users
                select new UserListItemDto(user);

            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    rs => routeStationIds.Contains(rs.Id)));
            IEnumerable<Guid> stationIds = routeStations.Select(rs => rs.StationId);
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));

            IEnumerable<Guid> vehicleTypeIds = bookings.Select(b => b.VehicleTypeId);
            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes
                .GetAllAsync(query => query.Where(
                    v => vehicleTypeIds.Contains(v.Id)));

            IEnumerable<BookingListItemDto> dtos =
                from booking in bookings
                join customer in userDtos
                    on booking.CustomerId equals customer.Id
                join startRouteStation in routeStations
                    on booking.StartRouteStationId equals startRouteStation.Id
                join startStation in stations
                    on startRouteStation.StationId equals startStation.Id
                join endRouteStation in routeStations
                    on booking.EndRouteStationId equals endRouteStation.Id
                join endStation in stations
                    on endRouteStation.StationId equals endStation.Id
                join vehicleType in vehicleTypes
                    on booking.VehicleTypeId equals vehicleType.Id
                select new BookingListItemDto(
                    booking, customer,
                    new RouteStationListItemDto(startRouteStation, startStation),
                    new RouteStationListItemDto(endRouteStation, endStation),
                    vehicleType
                    );

            return dtos;
        }
    }
}
