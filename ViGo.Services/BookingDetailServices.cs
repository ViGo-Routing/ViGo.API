using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.DTOs.BookingDetails;
using ViGo.DTOs.Routes;
using ViGo.DTOs.Stations;
using ViGo.DTOs.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;

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
            Route driverRoute = await work.Routes.GetAsync(bookingDetail.DriverRouteId);

            IEnumerable<Guid> stationIds = (new List<Guid>
            {
                customerRoute.StartStationId,
                customerRoute.EndStationId,
                driverRoute.StartStationId,
                driverRoute.EndStationId
            }).Distinct();

            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));

            RouteListItemDto customerRouteDto = new RouteListItemDto(
                customerRoute,
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(customerRoute.StartStationId)), 1),
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(customerRoute.EndStationId)), 2));

            RouteListItemDto driverRouteDto = new RouteListItemDto(
                driverRoute,
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.StartStationId)), 1),
                new StationListItemDto(
                    stations.SingleOrDefault(s => s.Id.Equals(driverRoute.EndStationId)), 2));

            BookingDetailListItemDto dto = new BookingDetailListItemDto(
                bookingDetail, driverDto, customerRouteDto, driverRouteDto);

            return dto;
        
        }
    }
}
