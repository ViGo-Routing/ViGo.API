using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.Stations;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class StationServices : BaseServices
    {
        public StationServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<StationViewModel?> GetStationAsync(Guid stationId,
            CancellationToken cancellationToken)
        {
            Station station = await work.Stations.GetAsync(stationId, cancellationToken: cancellationToken);
            if (station == null)
            {
                return null;
            }

            StationViewModel model = new StationViewModel(station);
            return model;

        }

        public async Task<IEnumerable<StationViewModel>> GetMetroStationsAsync(
            CancellationToken cancellationToken)
        {
            IEnumerable<Station> stations = await work.Stations.GetAllAsync(
                query => query.Where(
                    s => s.Type == Domain.Enumerations.StationType.METRO),
                cancellationToken: cancellationToken);

            IEnumerable<StationViewModel> models =
                from station in stations
                select new StationViewModel(station);
            return models;
        }
    }
}
