using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.DTOs.RouteRoutines;
using ViGo.DTOs.Routes;
using ViGo.DTOs.RouteStations;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class RouteServices : BaseServices<Route>
    {
        public RouteServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<Route> CreateRouteAsync(RouteCreateEditDto dto)
        {
            dto.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Tên tuyến đường không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Tên tuyến đường phải có từ 5 kí tự trở lên!",
                maxLength: 255,
                maxLengthErrorMessage: "Tên tuyến đường không được vượt quá 255 kí tự!"
                );
            if (dto.Distance <= 0)
            {
                throw new ApplicationException("Khoảng cách tuyến đường phải lớn hơn 0!");
            }
            if (dto.Duration <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển của tuyến đường phải lớn hơn 0!");
            }
            IsValidStation(dto.StartStation, "Điểm bắt đầu");
            IsValidStation(dto.EndStation, "Điểm kết thúc");

            if (dto.RouteRoutines.Count == 0)
            {
                throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            }
            foreach (RouteRoutineCreateEditDto routine in dto.RouteRoutines)
            {
                IsValidRoutine(routine);
            }
            await IsValidRoutines(dto.RouteRoutines);

            // Find Start Station existance
            Station startStation = await work.Stations.GetAsync(
                s => s.Longtitude == dto.StartStation.Longtitude
                && s.Latitude == dto.StartStation.Latitude
                && s.Status == StationStatus.ACTIVE);
            if (startStation == null)
            {
                startStation = new Station
                {
                    Longtitude = dto.StartStation.Longtitude,
                    Latitude = dto.StartStation.Latitude,
                    Name = dto.StartStation.Name,
                    Address = dto.StartStation.Address,
                    Status = StationStatus.ACTIVE
                };
                await work.Stations.InsertAsync(startStation);
            }

            // Find End Station existance
            Station endStation = await work.Stations.GetAsync(
                s => s.Longtitude == dto.EndStation.Longtitude
                && s.Latitude == dto.EndStation.Latitude
                && s.Status == StationStatus.ACTIVE);
            if (endStation == null)
            {
                endStation = new Station
                {
                    Longtitude = dto.EndStation.Longtitude,
                    Latitude = dto.EndStation.Latitude,
                    Name = dto.EndStation.Name,
                    Address = dto.EndStation.Address,
                    Status = StationStatus.ACTIVE
                };
                await work.Stations.InsertAsync(endStation);
            }

            // Create Route
            Route route = new Route
            {
                UserId = IdentityUtilities.GetCurrentUserId(),
                Name = dto.Name,
                StartStationId = startStation.Id,
                EndStationId = endStation.Id,
                Distance = dto.Distance,
                Duration = dto.Duration,
                Status = RouteStatus.ACTIVE
            };
            await work.Routes.InsertAsync(route);

            // Create RouteStations
            IList<RouteStation> routeStations = new List<RouteStation>()
            {
                // Start Station
                new RouteStation
                {
                    RouteId = route.Id,
                    StationId = startStation.Id,
                    StationIndex = 1,
                    DistanceFromFirstStation = 0,
                    DurationFromFirstStation = 0,
                    Status = RouteStationStatus.ACTIVE
                },

                // End Station
                new RouteStation
                {
                    RouteId = route.Id,
                    StationId = endStation.Id,
                    StationIndex = 2,
                    DistanceFromFirstStation = dto.Distance,
                    DurationFromFirstStation = dto.Duration,
                    Status = RouteStationStatus.ACTIVE
                }
            };
            await work.RouteStations.InsertAsync(routeStations);

            // Create RouteRoutine
            IList<RouteRoutine> routeRoutines =
                (from routine in dto.RouteRoutines
                 select new RouteRoutine
                 {
                     RouteId = route.Id,
                     StartDate = routine.StartDate.ToDateTime(TimeOnly.MinValue),
                     StartTime = routine.StartTime.ToTimeSpan(),
                     EndDate = routine.EndDate.ToDateTime(TimeOnly.MinValue),
                     EndTime = routine.EndTime.ToTimeSpan(),
                     Status = RouteRoutineStatus.ACTIVE
                 }).ToList();
            await work.RouteRoutines.InsertAsync(routeRoutines);

            await work.SaveChangesAsync();

            routeStations.ToList()
                .ForEach(rs => rs.Station = null);
            route.RouteStations = routeStations;
            route.RouteRoutines = routeRoutines;

            route.EndStation.RouteStations = null;
            route.StartStation.RouteStations = null;

            return route;
        }

        #region Validation
        private void IsValidStation(RouteStationCreateEditDto station, string stationName)
        {
            stationName = stationName.ToLower().Trim();

            if (station == null)
            {
                throw new ApplicationException(stationName[0].ToString().ToUpper() +
                    stationName.Substring(1) + " của tuyến đường không hợp lệ!");
            }
            station.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Tên " + stationName + " không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Tên " + stationName + " phải có từ 5 kí tự trở lên!",
                maxLength: 255,
                maxLengthErrorMessage: "Tên " + stationName + " không được vượt quá 255 kí tự!"
                );

            station.Address.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Địa chỉ " + stationName + " không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Địa chỉ " + stationName + " phải có từ 5 kí tự trở lên!",
                maxLength: 500,
                maxLengthErrorMessage: "Địa chỉ " + stationName + " không được vượt quá 255 kí tự!"
                );
        }

        private void IsValidRoutine(RouteRoutineCreateEditDto routine)
        {
            DateTime startDateTime = DateTimeUtilities
                .ToDateTime(routine.StartDate, routine.StartTime);
            DateTime endDateTime = DateTimeUtilities.
                ToDateTime(routine.EndDate, routine.EndTime);

            DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();
            startDateTime.DateTimeValidate(
                minimum: vnNow,
                minErrorMessage: $"Thời gian bắt đầu lịch trình ở quá khứ (ngày: " +
                $"{routine.StartDate.ToShortDateString()}, " +
                $"giờ: {routine.StartTime.ToShortTimeString()})",
                maximum: endDateTime,
                maxErrorMessage: $"Thời gian kết thúc lịch trình không hợp lệ (ngày: " +
                $"{routine.EndDate.ToShortDateString()}, " +
                $"giờ: {routine.EndTime.ToShortTimeString()})"
                );

        }

        private async Task IsValidRoutines(IList<RouteRoutineCreateEditDto> routines)
        {
            IList<DateTimeRange> routineRanges =
                (from routine in routines
                 select new DateTimeRange(
                     DateTimeUtilities
                 .ToDateTime(routine.StartDate, routine.StartTime),
                     DateTimeUtilities.
                 ToDateTime(routine.EndDate, routine.EndTime)
                     )).ToList();
            routineRanges = routineRanges.OrderBy(r => r.StartDateTime).ToList();

            for (int i = 0; i < routineRanges.Count - 1; i++)
            {
                DateTimeRange first = routineRanges[i];
                DateTimeRange second = routineRanges[i + 1];

                first.IsOverlap(second,
                    $"Hai khoảng thời gian lịch trình đã bị trùng lặp " +
                    $"({first.StartDateTime} - {first.EndDateTime} và " +
                    $"{second.StartDateTime} - {second.EndDateTime})");
            }

            IEnumerable<Route> routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => r.UserId.Equals(IdentityUtilities.GetCurrentUserId())
                    && r.Status == RouteStatus.ACTIVE));
            IEnumerable<Guid> routeIds = routes.Select(r => r.Id);

            IEnumerable<RouteRoutine> currentRouteRoutines =
                await work.RouteRoutines.GetAllAsync(
                    query => query.Where(
                        r => routeIds.Contains(r.RouteId)
                        && r.Status == RouteRoutineStatus.ACTIVE)
            );

            IEnumerable<DateTimeRange> currentRanges =
                from routine in currentRouteRoutines
                select new DateTimeRange(
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.StartDate), TimeOnly.FromTimeSpan(routine.StartTime)),
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.EndDate), TimeOnly.FromTimeSpan(routine.EndTime))
                );
            currentRanges = currentRanges.OrderBy(r => r.StartDateTime);

            if (!(routineRanges.Last().EndDateTime < currentRanges.First().StartDateTime
                || routineRanges.First().StartDateTime > currentRanges.Last().EndDateTime
                )
                )
            {
                IList<DateTimeRange> finalRanges = routineRanges.Union(currentRanges)
                .OrderBy(r => r.StartDateTime).ToList();
                //int firstIndex = finalRanges.IndexOf(routineRanges.First());
                //int secondIndex = finalRanges.IndexOf()

                for (int i = 0; i < finalRanges.Count - 1; i++)
                {
                    DateTimeRange current = finalRanges[i];
                    DateTimeRange added = finalRanges[i + 1];

                    if (currentRanges.Contains(added))
                    {
                        // finalRanges[i + 1] is current
                        current = finalRanges[i + 1];
                        added = finalRanges[i];
                    }
                    current.IsOverlap(added,
                        $"Hai khoảng thời gian lịch trình đã bị trùng lặp với lịch trình " +
                        $"đã được lưu trước đó " +
                        $"(Lịch trình đã được lưu trước đó: {current.StartDateTime} - {current.EndDateTime} và " +
                        $"lịch trình mới: {added.StartDateTime} - {added.EndDateTime})");
                }
            }

            

        }
        #endregion
    }
}
