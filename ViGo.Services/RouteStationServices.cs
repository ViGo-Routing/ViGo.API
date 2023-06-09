using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.RouteStations;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class RouteStationServices : BaseServices<RouteStation>
    {
        public RouteStationServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<RouteStationViewModel?> GetRouteStationAsync(Guid routeStationId)
        {
            RouteStation routeStation = await work.RouteStations
                .GetAsync(routeStationId);
            if (routeStation == null)
            {
                return null;
            }

            Station station = await work.Stations.GetAsync(routeStation.StationId);

            RouteStationViewModel model = new RouteStationViewModel(routeStation,
                station
                );
            return model;
        }

        public async Task<IEnumerable<RouteStationViewModel>>
            GetRouteStationsAsync(Guid routeId)
        {
            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    rs => rs.RouteId.Equals(routeId)));

            IEnumerable<Guid> stationIds = routeStations.Select(r => r.StationId);
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));

            IEnumerable<RouteStationViewModel> models =
                from routeStation in routeStations
                join station in stations
                    on routeStation.StationId equals station.Id
                select new RouteStationViewModel(routeStation, station);
            return models;
        }
    }
}
