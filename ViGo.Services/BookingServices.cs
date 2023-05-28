using Google.Apis.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.BookingDetails;
using ViGo.Models.Bookings;
using ViGo.Models.RouteStations;
using ViGo.Models.Users;
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
                    b => b.EndRouteStationId))
                .Distinct();

            IEnumerable<User> users = await work.Users
                .GetAllAsync(query => query.Where(
                    u => customerIds.Contains(u.Id)));
            IEnumerable<UserListItemDto> userDtos =
                from user in users
                select new UserListItemDto(user);

            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    rs => routeStationIds.Contains(rs.Id)));
            IEnumerable<Guid> stationIds = 
                routeStations.Select(rs => rs.StationId)
                .Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));

            IEnumerable<Guid> vehicleTypeIds = bookings.Select(b => b.VehicleTypeId).Distinct();
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

        public async Task<BookingListItemDto?> GetBookingAsync(Guid bookingId)
        {
            Booking booking = await work.Bookings.GetAsync(bookingId);
            if (booking == null)
            {
                return null;
            }

            User customer = await work.Users.GetAsync(booking.CustomerId);
            UserListItemDto customerDto = new UserListItemDto(customer);

            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    rs => rs.Id.Equals(booking.StartRouteStationId)
                    || rs.Id.Equals(booking.EndRouteStationId)));
            IEnumerable<Guid> stationIds = routeStations.Select(rs => rs.StationId).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));

            RouteStation startStation = routeStations.SingleOrDefault(rs => rs.Id.Equals(booking.StartRouteStationId));
            RouteStationListItemDto startStationDto = new RouteStationListItemDto(
                startStation, stations.SingleOrDefault(s => s.Id.Equals(startStation.StationId))
                );

            RouteStation endStation = routeStations.SingleOrDefault(rs => rs.Id.Equals(booking.EndRouteStationId));
            RouteStationListItemDto endStationDto = new RouteStationListItemDto(
                endStation, stations.SingleOrDefault(s => s.Id.Equals(endStation.StationId)));

            VehicleType vehicleType = await work.VehicleTypes.GetAsync(booking.VehicleTypeId);

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    bd => bd.BookingId.Equals(booking.Id)));

            IEnumerable<Guid?> driverIds = bookingDetails.Select(bd => bd.DriverId);
            driverIds = driverIds.Where(d => d.HasValue)
                .Distinct();
            IEnumerable<User> drivers = await work.Users.GetAllAsync(
                query => query.Where(
                    u => driverIds.Contains(u.Id)));

            IList<BookingDetailListItemDto> bookingDetailDtos = new List<BookingDetailListItemDto>();
            foreach (BookingDetail bookingDetail in bookingDetails)
            {
                UserListItemDto? driver = null;
                if (bookingDetail.DriverId.HasValue)
                {
                    driver = new UserListItemDto(
                        drivers.SingleOrDefault(
                        d => d.Id.Equals(bookingDetail.DriverId.Value)));
                }
                bookingDetailDtos.Add(new BookingDetailListItemDto(bookingDetail, driver));
            }

            BookingListItemDto dto = new BookingListItemDto(booking,
                customerDto, startStationDto, endStationDto, vehicleType, bookingDetailDtos);

            return dto;
        }
    }
}
