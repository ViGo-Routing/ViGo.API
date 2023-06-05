using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Routes;
using ViGo.Models.RouteStations;
using ViGo.Models.Stations;
using ViGo.Models.Users;
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

        public async Task<Route> CreateRouteAsync(RouteCreateEditModel dto)
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
            foreach (RouteRoutineCreateEditModel routine in dto.RouteRoutines)
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
            route.RouteRoutines = routeRoutines.OrderBy(r => r.StartDate)
                .ThenBy(r => r.StartTime).ToList();

            route.EndStation.RouteStations = null;
            route.StartStation.RouteStations = null;

            return route;
        }

        public async Task<IEnumerable<RouteViewModel>> GetRoutesAsync(Guid? userId = null)
        {
            IEnumerable<Route> routes = new List<Route>();

            if (userId.HasValue)
            {
                routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => r.UserId.Equals(userId)));
            } else
            {
                routes = await work.Routes
                    .GetAllAsync();
            }

            if (routes.Any())
            {
                IEnumerable<Guid> routeIds = routes.Select(r => r.Id);

                IEnumerable<RouteStation> routeStations = await work.RouteStations
                    .GetAllAsync(query => query.Where(
                        s => routeIds.Contains(s.RouteId)));

                IEnumerable<Guid> stationIds = routeStations.Select(s => s.StationId).Distinct();
                IEnumerable<Station> stations = await work.Stations
                    .GetAllAsync(query => query.Where(
                        s => stationIds.Contains(s.Id)));

                //IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines
                //    .GetAllAsync(query => query.Where(
                //        r => routeIds.Contains(r.RouteId)));

                IEnumerable<User> users = new List<User>();
                User? user = null;

                if (userId.HasValue)
                {
                    user = await work.Users.GetAsync(userId.Value);
                }
                else
                {
                    IEnumerable<Guid> userIds = routes.Select(r => r.UserId).Distinct();
                    users = await work.Users.GetAllAsync(
                        query => query.Where(
                            u => userIds.Contains(u.Id)));
                }

                IList<RouteViewModel> dtos = new List<RouteViewModel>();
                foreach (Route route in routes)
                {
                    // Routine
                    //IEnumerable<RouteRoutine> _routines = routeRoutines
                    //    .Where(r => r.RouteId.Equals(route.Id));

                    //IEnumerable<RouteRoutineListItemDto> routineDtos =
                    //    (from routine in _routines
                    //    select new RouteRoutineListItemDto(routine))
                    //    .OrderBy(r => r.StartDate)
                    //    .ThenBy(r => r.StartTime);

                    // Stations
                    Station startStation = stations.SingleOrDefault(
                        s => s.Id.Equals(route.StartStationId));
                    StationViewModel startStationDto = new StationViewModel(
                        startStation, 1
                        );

                    Station endStation = stations.SingleOrDefault(
                        s => s.Id.Equals(route.EndStationId));
                    StationViewModel endStationDto = new StationViewModel(
                        endStation, 2
                        );

                    User? routeUser = null;

                    if (userId.HasValue)
                    {
                        routeUser = user;
                    }
                    else
                    {
                        routeUser = users.SingleOrDefault(
                            u => u.Id.Equals(route.UserId));
                    }

                    UserViewModel userViewModel = new UserViewModel(routeUser);

                    dtos.Add(new RouteViewModel(
                        route,
                        startStationDto,
                        endStationDto,
                        userViewModel));
                }

                return dtos;
            }

            return new List<RouteViewModel>();

        }

        public async Task<RouteViewModel> GetRouteAsync(Guid routeId)
        {
            Route route = await work.Routes.GetAsync(routeId);
            if (route == null)
            {
                throw new ApplicationException("Tuyến đường không tồn tại!!");
            }

            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    s => s.RouteId.Equals(routeId)));

            IEnumerable<Guid> stationIds = routeStations.Select(s => s.StationId).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)));

            IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(routeId)));


            // Routine
            IEnumerable<RouteRoutineViewModel> routineDtos =
                (from routine in routeRoutines
                 select new RouteRoutineViewModel(routine))
                .OrderBy(r => r.StartDate)
                .ThenBy(r => r.StartTime);

            // Stations
            Station startStation = stations.SingleOrDefault(
                    s => s.Id.Equals(route.StartStationId));
            StationViewModel startStationDto = new StationViewModel(
                startStation, 1
                );

            Station endStation = stations.SingleOrDefault(
                s => s.Id.Equals(route.EndStationId));
            StationViewModel endStationDto = new StationViewModel(
                endStation, 2
                );

            // Route Stations
            IEnumerable<RouteStationViewModel> routeStationDtos =
                (from routeStation in routeStations
                 join station in stations
                    on routeStation.StationId equals station.Id
                 select new RouteStationViewModel(routeStation, station))
                 .OrderBy(s => s.StationIndex);

            // User
            User user = await work.Users.GetAsync(route.UserId);
            UserViewModel userViewModel = new UserViewModel(user);
            return new RouteViewModel(
                    route,
                    startStationDto,
                    endStationDto, 
                    routineDtos, routeStationDtos, userViewModel);

        }

        public async Task<Route> UpdateRouteAsync(RouteCreateEditModel updateDto)
        {
            Route route = await work.Routes.GetAsync(updateDto.Id);
            if (route == null)
            {
                throw new ApplicationException("Không tìm thấy tuyến đường! Vui lòng kiểm tra lại thông tin");
            }

            if (!route.UserId.Equals(updateDto.UserId))
            {
                throw new ApplicationException("Thông tin tuyến đường người dùng không trùng khớp! Vui lòng kiểm tra lại");
            }

            // Not tested yet
            // TODO Code
            // Check for Booking
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    b => b.DriverRouteId.Equals(updateDto.UserId)
                    || b.CustomerRouteId.Equals(updateDto.UserId)));
            if (bookingDetails.Any())
            {
                throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể thay đổi trạng thái tuyến đường");
            }

            updateDto.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Tên tuyến đường không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Tên tuyến đường phải có từ 5 kí tự trở lên!",
                maxLength: 255,
                maxLengthErrorMessage: "Tên tuyến đường không được vượt quá 255 kí tự!"
                );
            if (updateDto.Distance <= 0)
            {
                throw new ApplicationException("Khoảng cách tuyến đường phải lớn hơn 0!");
            }
            if (updateDto.Duration <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển của tuyến đường phải lớn hơn 0!");
            }
            IsValidStation(updateDto.StartStation, "Điểm bắt đầu");
            IsValidStation(updateDto.EndStation, "Điểm kết thúc");

            if (updateDto.RouteRoutines.Count == 0)
            {
                throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            }
            foreach (RouteRoutineCreateEditModel routine in updateDto.RouteRoutines)
            {
                IsValidRoutine(routine);
            }
            await IsValidRoutines(updateDto.RouteRoutines, true, updateDto.Id);

            // Find Start Station existance
            Station startStation = await work.Stations.GetAsync(
                s => s.Longtitude == updateDto.StartStation.Longtitude
                && s.Latitude == updateDto.StartStation.Latitude
                && s.Status == StationStatus.ACTIVE);
            if (startStation == null)
            {
                startStation = new Station
                {
                    Longtitude = updateDto.StartStation.Longtitude,
                    Latitude = updateDto.StartStation.Latitude,
                    Name = updateDto.StartStation.Name,
                    Address = updateDto.StartStation.Address,
                    Status = StationStatus.ACTIVE
                };
                await work.Stations.InsertAsync(startStation);
            }

            // Find End Station existance
            Station endStation = await work.Stations.GetAsync(
                s => s.Longtitude == updateDto.EndStation.Longtitude
                && s.Latitude == updateDto.EndStation.Latitude
                && s.Status == StationStatus.ACTIVE);
            if (endStation == null)
            {
                endStation = new Station
                {
                    Longtitude = updateDto.EndStation.Longtitude,
                    Latitude = updateDto.EndStation.Latitude,
                    Name = updateDto.EndStation.Name,
                    Address = updateDto.EndStation.Address,
                    Status = StationStatus.ACTIVE
                };
                await work.Stations.InsertAsync(endStation);
            }

            // Update Route
            route.Name = updateDto.Name;
            route.StartStationId = startStation.Id;
            route.EndStationId = endStation.Id;
            route.Distance = updateDto.Distance;
            route.Duration = updateDto.Duration;

            await work.Routes.UpdateAsync(route);

            // Update RouteStations
            IEnumerable<RouteStation> currentRouteStations
                = await work.RouteStations.GetAllAsync(query => query.Where(
                    s => s.RouteId.Equals(route.Id)));
            foreach (RouteStation routeStation in currentRouteStations)
            {
                await work.RouteStations.DeleteAsync(routeStation, isSoftDelete: false);
            }

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
                    DistanceFromFirstStation = updateDto.Distance,
                    DurationFromFirstStation = updateDto.Duration,
                    Status = RouteStationStatus.ACTIVE
                }
            };
            await work.RouteStations.InsertAsync(routeStations);

            // Update RouteRoutine
            IEnumerable<RouteRoutine> currentRoutines
                = await work.RouteRoutines.GetAllAsync(query => query.Where(
                    s => s.RouteId.Equals(route.Id)));
            foreach (RouteRoutine routeRoutine in currentRoutines)
            {
                await work.RouteRoutines.DeleteAsync(routeRoutine, isSoftDelete: false);
            }

            IList<RouteRoutine> routeRoutines =
                (from routine in updateDto.RouteRoutines
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
            route.RouteRoutines = routeRoutines.OrderBy(r => r.StartDate)
                .ThenBy(r => r.StartTime).ToList();
            route.EndStation = endStation;
            route.StartStation = startStation;

            return route;

        }

        public async Task<Route> ChangeRouteStatusAsync(Guid routeId, RouteStatus newStatus)
        {
            if (!Enum.IsDefined(newStatus))
            {
                throw new ApplicationException("Trạng thái tuyến đường không hợp lệ!");
            }

            Route route = await work.Routes.GetAsync(routeId);
            if (route == null)
            {
                throw new ApplicationException("Không tìm thấy tuyến đường được chỉ định!!");
            }

            // Check for Booking
            // Not tested yet
            // TODO Code
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    b => b.DriverRouteId.Equals(route.UserId)
                    || b.CustomerRouteId.Equals(route.UserId)));
            if (bookingDetails.Any())
            {
                throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể thay đổi trạng thái tuyến đường");
            }

            if (route.Status != newStatus)
            {
                route.Status = newStatus;
                await work.Routes.UpdateAsync(route);
                await work.SaveChangesAsync();
            }

            return route;
        }

        #region Validation
        private void IsValidStation(RouteStationCreateEditModel station, string stationName)
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

        private void IsValidRoutine(RouteRoutineCreateEditModel routine)
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

        private async Task IsValidRoutines(IList<RouteRoutineCreateEditModel> routines,
            bool isUpdate = false, Guid? routeId = null)
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

            if (isUpdate)
            {
                if (routeId == null)
                {
                    throw new Exception("Thiếu thông tin tuyến đường! Vui lòng kiểm tra lại");
                }
                routeIds = routeIds.Where(r => !r.Equals(routeId.Value));
            }

            if (!routeIds.Any())
            {
                return;
            }

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
