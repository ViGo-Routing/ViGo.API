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

            IEnumerable<Guid> customerIds = bookings.Select(b => b.CustomerId);
            IEnumerable<Guid> routeStationIds = bookings.Select(
                b => b.StartRouteStationId).Concat(bookings.Select(
                    b => b.EndRouteStationId))
                .Distinct();

            IEnumerable<User> users = await work.Users
                .GetAllAsync(query => query.Where(
                    u => customerIds.Contains(u.Id)), cancellationToken: cancellationToken);
            IEnumerable<UserViewModel> userDtos =
                from user in users
                select new UserViewModel(user);

            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    rs => routeStationIds.Contains(rs.Id)), cancellationToken: cancellationToken);
            IEnumerable<Guid> stationIds =
                routeStations.Select(rs => rs.StationId)
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
                select new BookingViewModel(
                    booking, customer,
                    new RouteStationViewModel(startRouteStation, startStation),
                    new RouteStationViewModel(endRouteStation, endStation),
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

            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    rs => rs.Id.Equals(booking.StartRouteStationId)
                    || rs.Id.Equals(booking.EndRouteStationId)), cancellationToken: cancellationToken);
            IEnumerable<Guid> stationIds = routeStations.Select(rs => rs.StationId).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

            RouteStation startStation = routeStations.SingleOrDefault(rs => rs.Id.Equals(booking.StartRouteStationId));
            RouteStationViewModel startStationDto = new RouteStationViewModel(
                startStation, stations.SingleOrDefault(s => s.Id.Equals(startStation.StationId))
                );

            RouteStation endStation = routeStations.SingleOrDefault(rs => rs.Id.Equals(booking.EndRouteStationId));
            RouteStationViewModel endStationDto = new RouteStationViewModel(
                endStation, stations.SingleOrDefault(s => s.Id.Equals(endStation.StationId)));

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
                customerDto, startStationDto, endStationDto, vehicleType);
            //BookingViewModel dto = new BookingViewModel(booking);

            return dto;
        }
    }
}
