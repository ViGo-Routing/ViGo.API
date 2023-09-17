using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Data;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.Routes;
using ViGo.Models.Stations;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class RouteServices : BaseServices
    {
        public RouteServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<Route> CreateRouteAsync(RouteCreateModel dto,
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin()
                && !IdentityUtilities.IsStaff())
            {
                //if (!dto.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                //{
                //    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!");
                //}
                dto.UserId = IdentityUtilities.GetCurrentUserId();
            }

            User user = await work.Users.GetAsync(
                u => u.Id.Equals(dto.UserId) && u.Status == UserStatus.ACTIVE,
                cancellationToken: cancellationToken);
            if (user is null || user.Role != UserRole.CUSTOMER)
            {
                throw new ApplicationException("Thông tin người dùng không hợp lệ!!");
            }

            dto.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Tên tuyến đường không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Tên tuyến đường phải có từ 5 kí tự trở lên!",
                maxLength: 255,
                maxLengthErrorMessage: "Tên tuyến đường không được vượt quá 255 kí tự!"
                );

            if (!Enum.IsDefined(dto.Type))
            {
                throw new ApplicationException("Loại tuyến đường không hợp lệ!!");
            }
            if (!Enum.IsDefined(dto.RoutineType))
            {
                throw new ApplicationException("Loại lịch trình đi không hợp lệ!!");
            }

            if (dto.StartStation == null || dto.EndStation == null)
            {
                throw new ApplicationException("Tuyến đường chưa được thiết lập điểm bắt đầu và kết thúc!!");
            }

            if (dto.Distance <= 0)
            {
                throw new ApplicationException("Khoảng cách tuyến đường phải lớn hơn 0!");
            }
            if (dto.Distance <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển của tuyến đường phải lớn hơn 0!");
            }
            IsValidStation(dto.StartStation, "Điểm bắt đầu");
            IsValidStation(dto.EndStation, "Điểm kết thúc");
            //if (dto.StartStation != null)
            //{
            //}
            //if (dto.EndStation != null)
            //{
            //}

            //if (dto.RouteRoutines.Count == 0)
            //{
            //    throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            //}
            //foreach (RouteRoutineCreateEditModel routine in dto.RouteRoutines)
            //{
            //    IsValidRoutine(routine);
            //}
            //await IsValidRoutines(dto.RouteRoutines);

            //Station? startStation = null;

            // Find Start Station existance
            Station? startStation = await work.Stations.GetAsync(
             s => ((s.Longitude == dto.StartStation.Longitude
             && s.Latitude == dto.StartStation.Latitude)
             || s.Address.ToLower().Equals(dto.StartStation.Address.ToLower()))
             && s.Status == StationStatus.ACTIVE,
             cancellationToken: cancellationToken);
            if (startStation == null)
            {
                startStation = new Station
                {
                    Longitude = dto.StartStation.Longitude,
                    Latitude = dto.StartStation.Latitude,
                    Name = dto.StartStation.Name,
                    Address = dto.StartStation.Address,
                    Status = StationStatus.ACTIVE,
                    Type = StationType.OTHER
                };
                await work.Stations.InsertAsync(startStation, cancellationToken: cancellationToken);
            }

            // Find End Station existance
            Station? endStation = await work.Stations.GetAsync(
                s => ((s.Longitude == dto.EndStation.Longitude
                && s.Latitude == dto.EndStation.Latitude)
                || s.Address.ToLower().Equals(dto.EndStation.Address.ToLower()))
                && s.Status == StationStatus.ACTIVE,
                cancellationToken: cancellationToken);
            if (endStation == null)
            {
                endStation = new Station
                {
                    Longitude = dto.EndStation.Longitude,
                    Latitude = dto.EndStation.Latitude,
                    Name = dto.EndStation.Name,
                    Address = dto.EndStation.Address,
                    Status = StationStatus.ACTIVE,
                    Type = StationType.OTHER
                };
                await work.Stations.InsertAsync(endStation, cancellationToken: cancellationToken);
            }

            // Create Route
            Route route = new Route
            {
                UserId = dto.UserId.Value,
                Name = dto.Name,
                StartStationId = startStation.Id,
                EndStationId = endStation.Id,
                Distance = dto.Distance,
                Duration = Math.Round(dto.Duration, 2),
                RoutineType = dto.RoutineType,
                Type = dto.Type,
                Status = RouteStatus.ACTIVE
            };

            if (dto.Type == RouteType.ROUND_TRIP)
            {
                Route roundTripRoute = new Route
                {
                    UserId = dto.UserId.Value,
                    Name = dto.Name,
                    StartStationId = endStation.Id,
                    EndStationId = startStation.Id,
                    Distance = dto.Distance,
                    Duration = dto.Duration,
                    RoutineType = dto.RoutineType,
                    Type = dto.Type,
                    Status = RouteStatus.ACTIVE
                };
                await work.Routes.InsertAsync(roundTripRoute, cancellationToken: cancellationToken);

                route.RoundTripRouteId = roundTripRoute.Id;

            }

            await work.Routes.InsertAsync(route, cancellationToken: cancellationToken);

            //// Create RouteStations
            //IList<RouteStation> routeStations = new List<RouteStation>();
            //if (startStation != null)
            //{
            //    routeStations.Add(
            //        // Start Station
            //        new RouteStation
            //        {
            //            RouteId = route.Id,
            //            StationId = startStation.Id,
            //            StationIndex = 1,
            //            DistanceFromFirstStation = 0,
            //            DurationFromFirstStation = 0,
            //            Status = RouteStationStatus.ACTIVE
            //        });
            //}
            //if (endStation != null)
            //{
            //    routeStations.Add(
            //        // End Station
            //        new RouteStation
            //        {
            //            RouteId = route.Id,
            //            StationId = endStation.Id,
            //            StationIndex = 2,
            //            DistanceFromFirstStation = dto.Distance,
            //            DurationFromFirstStation = dto.Duration,
            //            Status = RouteStationStatus.ACTIVE
            //        });
            //}
            //await work.RouteStations.InsertAsync(routeStations, cancellationToken: cancellationToken);

            // Create RouteRoutine
            //IList<RouteRoutine> routeRoutines =
            //    (from routine in dto.RouteRoutines
            //     select new RouteRoutine
            //     {
            //         RouteId = route.Id,
            //         StartDate = routine.StartDate.ToDateTime(TimeOnly.MinValue),
            //         StartTime = routine.StartTime.ToTimeSpan(),
            //         EndDate = routine.EndDate.ToDateTime(TimeOnly.MinValue),
            //         EndTime = routine.EndTime.ToTimeSpan(),
            //         Status = RouteRoutineStatus.ACTIVE
            //     }).ToList();
            //await work.RouteRoutines.InsertAsync(routeRoutines);

            await work.SaveChangesAsync(cancellationToken);

            //routeStations.ToList()
            //    .ForEach(rs => rs.Station = null);
            //route.RouteStations = routeStations;
            //route.RouteRoutines = routeRoutines.OrderBy(r => r.StartDate)
            //    .ThenBy(r => r.StartTime).ToList();

            //route.EndStation.RouteStations = null;
            //route.StartStation.RouteStations = null;

            return route;
        }

        public async Task<IPagedEnumerable<RouteViewModel>> GetRoutesAsync(Guid? userId,
            PaginationParameter pagination, RouteSortingParameters sorting,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Route> routes = new List<Route>();

            if (userId.HasValue)
            {
                routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => r.UserId.Equals(userId)), cancellationToken: cancellationToken);
            }
            else
            {
                routes = await work.Routes
                    .GetAllAsync(cancellationToken: cancellationToken);
            }

            routes = routes.Where(
                r =>
                {
                    if (r.Type == RouteType.ONE_WAY)
                    {
                        return true;
                    }
                    // ROUND_TRIP
                    if (r.RoundTripRouteId.HasValue)
                    {
                        return true;
                    }
                    return false;
                });

            routes = routes.Sort(sorting.OrderBy);

            int totalRecords = routes.Count();

            routes = routes.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            if (routes.Any())
            {
                IEnumerable<Guid> routeIds = routes.Select(r => r.Id);

                //IEnumerable<RouteStation> routeStations = await work.RouteStations
                //    .GetAllAsync(query => query.Where(
                //        s => routeIds.Contains(s.RouteId)), 
                //        cancellationToken: cancellationToken);

                IEnumerable<Guid> stationIds = routes.Select(s => s.StartStationId)
                    .Concat(routes.Select(s => s.EndStationId)).Distinct();
                IEnumerable<Station> stations = await work.Stations
                    .GetAllAsync(query => query.Where(
                        s => stationIds.Contains(s.Id)), includeDeleted: true,
                        cancellationToken: cancellationToken);

                //IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines
                //    .GetAllAsync(query => query.Where(
                //        r => routeIds.Contains(r.RouteId)));

                IEnumerable<User> users = new List<User>();
                User? user = null;

                if (userId.HasValue)
                {
                    user = await work.Users.GetAsync(userId.Value, cancellationToken: cancellationToken);
                }
                else
                {
                    IEnumerable<Guid> userIds = routes.Select(r => r.UserId).Distinct();
                    users = await work.Users.GetAllAsync(
                        query => query.Where(
                            u => userIds.Contains(u.Id)),
                        cancellationToken: cancellationToken);
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
                    StationViewModel startStationDto = new StationViewModel(startStation);

                    Station endStation = stations.SingleOrDefault(
                        s => s.Id.Equals(route.EndStationId));
                    StationViewModel endStationDto = new StationViewModel(endStation);

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

                return dtos.ToPagedEnumerable(pagination.PageNumber,
                    pagination.PageSize, totalRecords, context);
            }

            return new List<RouteViewModel>().ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize, 0, context);
            //IEnumerable<RouteViewModel> models =
            //    from route in routes
            //    select new RouteViewModel(route);

            //return models;

        }

        public async Task<RouteViewModel> GetRouteAsync(Guid routeId,
            CancellationToken cancellationToken)
        {
            Route route = await work.Routes.GetAsync(routeId, cancellationToken: cancellationToken);
            if (route == null)
            {
                throw new ApplicationException("Tuyến đường không tồn tại!!");
            }

            IEnumerable<Guid> stationIds = new List<Guid>
            {
                route.StartStationId,
                route.EndStationId
            };

            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), true, cancellationToken: cancellationToken);

            IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(routeId)), cancellationToken: cancellationToken);

            // Routine
            //IEnumerable<RouteRoutineViewModel> routineDtos =
            //    (from routine in routeRoutines
            //     select new RouteRoutineViewModel(routine))
            //    .OrderBy(r => r.RoutineDate)
            //    .ThenBy(r => r.StartTime);

            // Stations

            Station startStation = stations.SingleOrDefault(
                s => s.Id.Equals(route.StartStationId));
            StationViewModel startStationDto = new StationViewModel(startStation);


            Station endStation = stations.SingleOrDefault(
            s => s.Id.Equals(route.EndStationId));
            StationViewModel endStationDto = new StationViewModel(endStation);

            //// Route Stations
            //IEnumerable<RouteStationViewModel> routeStationDtos =
            //    (from routeStation in routeStations
            //     join station in stations
            //        on routeStation.StationId equals station.Id
            //     select new RouteStationViewModel(routeStation, station))
            //     .OrderBy(s => s.StationIndex);

            // User
            User user = await work.Users.GetAsync(route.UserId, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);

            // Round Trip
            RouteViewModel? roundTrip = null;
            if (route.Type == RouteType.ROUND_TRIP)
            {
                //if (route.RoundTripRouteId is null)
                //{
                //    throw new ApplicationException("Tuyến đường khứ hồi nhưng thiếu thông tin tuyến đường chiều về!");
                //}

                if (route.RoundTripRouteId != null)
                {
                    Route? roundTripRoute = await work.Routes.GetAsync(route.RoundTripRouteId.Value, cancellationToken: cancellationToken);
                    if (roundTripRoute is null)
                    {
                        throw new ApplicationException("Tuyến đường chiều về không tồn tại!!");
                    }

                    roundTrip = new RouteViewModel(roundTripRoute, endStationDto, startStationDto, userViewModel);
                }
            }
            return new RouteViewModel(
                    route,
                    startStationDto,
                    endStationDto,
                    userViewModel,
                    roundTrip);

            //RouteViewModel model = new RouteViewModel(route);
            //return model;

        }

        public async Task<Route> UpdateRouteAsync(RouteUpdateModel updateDto,
            bool isCalledFromBooking = false,
            CancellationToken cancellationToken = default)
        {
            //if (!updateDto.Id.HasValue)
            //{
            //    throw new ApplicationException("Thông tin tuyến đường không hợp lệ!!");
            //}
            Route route = await work.Routes.GetAsync(updateDto.Id, cancellationToken: cancellationToken);
            if (route == null)
            {
                throw new ApplicationException("Không tìm thấy tuyến đường! Vui lòng kiểm tra lại thông tin");
            }

            if (!route.UserId.Equals(updateDto.UserId))
            {
                throw new ApplicationException("Thông tin tuyến đường người dùng không trùng khớp! Vui lòng kiểm tra lại");
            }
            if (!IdentityUtilities.IsAdmin()
                && !IdentityUtilities.IsStaff())
            {
                if (!updateDto.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!");
                }
            }

            // Not tested yet
            // TODO Code
            // Check for Booking
            //IEnumerable<Booking> bookings = await work.Bookings
            //    .GetAllAsync(query => query.Where(
            //        b => b.CustomerRouteId.Equals(updateDto.Id)),
            //        cancellationToken: cancellationToken);
            //IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

            //IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
            //    .GetAllAsync(query => query.Where(
            //        b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
            //        /*|| (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id)*/) // Driver Route
            //        /*&& b.Status != BookingDetailStatus.CANCELLED*/),
            //        cancellationToken: cancellationToken);
            //if (bookingDetails.Any())
            //{
            //    throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể cập nhật thông tin tuyến đường");
            //}
            if (await HasDriver(route.Id, cancellationToken))
            {
                throw new ApplicationException("Hành trình đã có tài xế chọn nên không thể cập nhật thông tin!");
            }
            Booking? booking = await HasBooking(route.Id, cancellationToken);
            if (booking != null && !isCalledFromBooking)
            {
                //if (updateDto.RouteRoutines is null || updateDto.RouteRoutines.Count == 0)
                //{
                //    throw new ApplicationException("Hành trình đã được đặt lịch, vui lòng cập nhật cả lịch trình!");
                //}
                //if (updateDto.Type == RouteType.ROUND_TRIP)
                //{
                //    if (updateDto.RoundTripRoutines is null || updateDto.RoundTripRoutines.Count == 0)
                //    {
                //        throw new ApplicationException("Hành trình khứ hồi đã được đặt lịch, vui lòng cập nhật cả lịch trình!");
                //    }
                //}
                throw new ApplicationException("Tuyến đường đã được đặt lịch! Vui lòng sử dụng " +
                    "chức năng cập nhật Đặt lịch để cập nhật các thông tin cần thiết!");
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
            {
                updateDto.Name.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Tên tuyến đường không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Tên tuyến đường phải có từ 5 kí tự trở lên!",
                maxLength: 255,
                maxLengthErrorMessage: "Tên tuyến đường không được vượt quá 255 kí tự!"
                );

                route.Name = updateDto.Name;
            }

            if (updateDto.Distance <= 0)
            {
                throw new ApplicationException("Khoảng cách tuyến đường phải lớn hơn 0!");
            }
            else
            {
                route.Distance = updateDto.Distance;
            }
            if (updateDto.Duration <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển của tuyến đường phải lớn hơn 0!");
            }
            else
            {
                route.Duration = updateDto.Duration;
            }

            if (!Enum.IsDefined(updateDto.RoutineType))
            {
                throw new ApplicationException("Loại lịch trình đi không hợp lệ!!");
            }
            if (!Enum.IsDefined(updateDto.Type))
            {
                throw new ApplicationException("Loại tuyến đường không hợp lệ!!");
            }

            //IList<RouteStation> routeStations = new List<RouteStation>();
            //Station? startStation = null, endStation = null;

            //if (updateDto.RouteType == RouteType.EVERY_ROUTE_SPECIFIC_TIME ||
            //    updateDto.RouteType == RouteType.EVERY_ROUTE_EVERY_TIME)
            //{
            //    // No need to set up Start, End
            //    if (updateDto.StartStation != null || updateDto.EndStation != null)
            //    {
            //        throw new ApplicationException("Tuyến đường này không cần thiết lập điểm bắt đầu và kết thúc!");
            //    }
            //}
            //else
            //{
            //    //if (updateDto.StartStation == null || updateDto.EndStation == null)
            //    //{
            //    //    throw new ApplicationException("Tuyến đường chưa được thiết lập điểm bắt đầu và kết thúc!!");
            //    //}
            //    //IsValidStation(updateDto.StartStation, "Điểm bắt đầu");
            //    //IsValidStation(updateDto.EndStation, "Điểm kết thúc");

            //    if (updateDto.StartStation != null)
            //    {
            IsValidStation(updateDto.StartStation, "Điểm bắt đầu");

            // Find Start Station existance
            Station startStation = await work.Stations.GetAsync(
                s => ((s.Longitude == updateDto.StartStation.Longitude
                    && s.Latitude == updateDto.StartStation.Latitude)
                    || s.Address.ToLower().Equals(updateDto.StartStation.Address.ToLower()))
                && s.Status == StationStatus.ACTIVE,
                cancellationToken: cancellationToken);
            if (startStation == null)
            {
                startStation = new Station
                {
                    Longitude = updateDto.StartStation.Longitude,
                    Latitude = updateDto.StartStation.Latitude,
                    Name = updateDto.StartStation.Name,
                    Address = updateDto.StartStation.Address,
                    Status = StationStatus.ACTIVE,
                    Type = StationType.OTHER
                };
                await work.Stations.InsertAsync(startStation, cancellationToken: cancellationToken);
            }

            //routeStations.Add(
            //    // Start Station
            //    new RouteStation
            //    {
            //        RouteId = route.Id,
            //        StationId = startStation.Id,
            //        StationIndex = 1,
            //        DistanceFromFirstStation = 0,
            //        DurationFromFirstStation = 0,
            //        Status = RouteStationStatus.ACTIVE
            //    }
            //);
            route.StartStationId = startStation.Id;
            //}

            //if (updateDto.EndStation != null)
            //{
            IsValidStation(updateDto.EndStation, "Điểm kết thúc");

            // Find End Station existance
            Station endStation = await work.Stations.GetAsync(
               s => ((s.Longitude == updateDto.EndStation.Longitude
                   && s.Latitude == updateDto.EndStation.Latitude)
                   || s.Address.ToLower().Equals(updateDto.EndStation.Address.ToLower()))
               && s.Status == StationStatus.ACTIVE,
               cancellationToken: cancellationToken);
            if (endStation == null)
            {
                endStation = new Station
                {
                    Longitude = updateDto.EndStation.Longitude,
                    Latitude = updateDto.EndStation.Latitude,
                    Name = updateDto.EndStation.Name,
                    Address = updateDto.EndStation.Address,
                    Status = StationStatus.ACTIVE,
                    Type = StationType.OTHER
                };
                await work.Stations.InsertAsync(endStation, cancellationToken: cancellationToken);
                //}

                //routeStations.Add(
                //    // End Station
                //    new RouteStation
                //    {
                //        RouteId = route.Id,
                //        StationId = endStation.Id,
                //        StationIndex = 2,
                //        DistanceFromFirstStation = updateDto.Distance,
                //        DurationFromFirstStation = updateDto.Duration,
                //        Status = RouteStationStatus.ACTIVE
                //    }
                //);
                route.EndStationId = endStation.Id;
            }

            //if (updateDto.RouteRoutines.Count == 0)
            //{
            //    throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            //}
            //foreach (RouteRoutineCreateEditModel routine in updateDto.RouteRoutines)
            //{
            //    IsValidRoutine(routine);
            //}
            //await IsValidRoutines(updateDto.RouteRoutines, true, updateDto.Id);

            // RouteType
            Route? roundTrip = null;
            if (route.Type != updateDto.Type)
            {
                route.Type = updateDto.Type;

                if (updateDto.Type == RouteType.ONE_WAY)
                {
                    // Previous Route Type is ROUND_TRIP
                    // Delete the round trip one
                    Guid roundTripRouteId = route.RoundTripRouteId.Value;
                    roundTrip = await work.Routes.GetAsync(roundTripRouteId, cancellationToken: cancellationToken);

                    IEnumerable<RouteRoutine> routines = await work.RouteRoutines.GetAllAsync(
                        query => query.Where(r => r.RouteId.Equals(roundTripRouteId)),
                        cancellationToken: cancellationToken);

                    if (routines.Any())
                    {
                        foreach (var routine in routines)
                        {
                            await work.RouteRoutines.DeleteAsync(routine,
                                isSoftDelete: true,
                                cancellationToken);
                        }
                    }
                    await work.Routes.DeleteAsync(roundTrip,
                        isSoftDelete: true,
                        cancellationToken);
                }
                else if (updateDto.Type == RouteType.ROUND_TRIP)
                {
                    // Previous Route Type is ONE_WAY
                    // Generate 1 more route
                    roundTrip = new Route
                    {
                        UserId = route.UserId,
                        Name = route.Name,
                        StartStationId = route.EndStationId,
                        EndStationId = route.StartStationId,
                        Distance = route.Distance,
                        Duration = route.Duration,
                        RoutineType = route.RoutineType,
                        Type = route.Type,
                        Status = RouteStatus.ACTIVE
                    };
                    await work.Routes.InsertAsync(roundTrip, cancellationToken: cancellationToken);

                    route.RoundTripRouteId = roundTrip.Id;
                }
            }
            else
            {
                // Same Route
                if (route.Type == RouteType.ROUND_TRIP)
                {
                    if (route.RoundTripRouteId.HasValue)
                    {
                        roundTrip = await work.Routes.GetAsync(route.RoundTripRouteId.Value, cancellationToken: cancellationToken);

                        roundTrip.Name = route.Name;
                        roundTrip.StartStationId = route.EndStationId;
                        roundTrip.EndStationId = route.StartStationId;
                        roundTrip.Distance = route.Distance;
                        roundTrip.Duration = route.Duration;
                        roundTrip.RoutineType = route.RoutineType;
                        roundTrip.Status = route.Status;

                        await work.Routes.UpdateAsync(roundTrip);
                    }
                    else
                    {
                        throw new ApplicationException("Đây là tuyến đường về thuộc tuyến đường khứ hồi! " +
                            "Không thể cập nhật tuyến đường này. Vui lòng cập nhật tuyến đường chính!");
                    }
                }
            }
            // Update Route
            await work.Routes.UpdateAsync(route);

            // Update RouteStations
            //if (routeStations.Count > 0)
            //{
            //    IEnumerable<RouteStation> currentRouteStations
            //    = await work.RouteStations.GetAllAsync(query => query.Where(
            //        s => s.RouteId.Equals(route.Id)), cancellationToken: cancellationToken);
            //    foreach (RouteStation routeStation in currentRouteStations)
            //    {
            //        await work.RouteStations.DeleteAsync(routeStation, isSoftDelete: false);
            //    }

            //    await work.RouteStations.InsertAsync(routeStations, cancellationToken: cancellationToken);
            //}

            // Update RouteRoutine
            //IEnumerable<RouteRoutine> currentRoutines
            //    = await work.RouteRoutines.GetAllAsync(query => query.Where(
            //        s => s.RouteId.Equals(route.Id)));
            //foreach (RouteRoutine routeRoutine in currentRoutines)
            //{
            //    await work.RouteRoutines.DeleteAsync(routeRoutine, isSoftDelete: false);
            //}

            //IList<RouteRoutine> routeRoutines =
            //    (from routine in updateDto.RouteRoutines
            //     select new RouteRoutine
            //     {
            //         RouteId = route.Id,
            //         StartDate = routine.StartDate.ToDateTime(TimeOnly.MinValue),
            //         StartTime = routine.StartTime.ToTimeSpan(),
            //         EndDate = routine.EndDate.ToDateTime(TimeOnly.MinValue),
            //         EndTime = routine.EndTime.ToTimeSpan(),
            //         Status = RouteRoutineStatus.ACTIVE
            //     }).ToList();
            //await work.RouteRoutines.InsertAsync(routeRoutines);

            //Route checkroute = await work.Routes.GetAsync(route.RoundTripRouteId.Value, cancellationToken: cancellationToken);

            await work.SaveChangesAsync(cancellationToken);
            //routeStations.ToList()
            //    .ForEach(rs => rs.Station = null);
            //route.RouteStations = routeStations;
            //route.RouteRoutines = routeRoutines.OrderBy(r => r.StartDate)
            //    .ThenBy(r => r.StartTime).ToList();

            if (!isCalledFromBooking)
            {
                route.EndStation = endStation;
                route.StartStation = startStation;
            }


            // Update for Booking and BookingDetail
            //Booking? currentBooking = await work.Bookings
            //    .GetAsync(b => b.CustomerRouteId.Equals(route.Id)
            //        && b.Status == BookingStatus.CONFIRMED, 
            //        cancellationToken: cancellationToken);
            //if (booking != null)
            //{
            //    // Has Booking
            //    // Routines must be updated as well

            //    if (roundTrip != null)
            //    {
            //        IsValidRoundTripRoutines(updateDto.RouteRoutines,
            //        updateDto.RoundTripRoutines);
            //    }

            //    RouteRoutineServices routeRoutineServices = new RouteRoutineServices(work, _logger);
            //    await routeRoutineServices.UpdateRouteRoutinesAsync(new RouteRoutineUpdateModel()
            //    {
            //        RouteId = route.Id,
            //        RouteRoutines = updateDto.RouteRoutines
            //    }, false, cancellationToken);

            //    if (roundTrip != null)
            //    {
            //        await routeRoutineServices.UpdateRouteRoutinesAsync(new RouteRoutineUpdateModel()
            //        {
            //            RouteId = roundTrip.Id,
            //            RouteRoutines = updateDto.RoundTripRoutines
            //        }, false, cancellationToken);
            //    }


            //}

            return route;

        }

        public async Task<Route> ChangeRouteStatusAsync(Guid routeId, RouteStatus newStatus,
            CancellationToken cancellationToken)
        {
            if (!Enum.IsDefined(newStatus))
            {
                throw new ApplicationException("Trạng thái tuyến đường không hợp lệ!");
            }

            Route route = await work.Routes.GetAsync(routeId, cancellationToken: cancellationToken);
            if (route is null)
            {
                throw new ApplicationException("Không tìm thấy tuyến đường được chỉ định!!");
            }

            if (!IdentityUtilities.IsAdmin()
                && !IdentityUtilities.IsStaff())
            {
                if (!route.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!");
                }
            }

            // Check for Booking
            // Not tested yet
            // TODO Code
            //IEnumerable<Booking> bookings = await work.Bookings
            //    .GetAllAsync(query => query.Where(
            //        b => b.CustomerRouteId.Equals(routeId)),
            //        cancellationToken: cancellationToken);
            //IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

            //IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
            //    .GetAllAsync(query => query.Where(
            //        b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
            //        /*|| (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id))*/ // Driver Route
            //        && b.Status != BookingDetailStatus.CANCELLED)),
            //        cancellationToken: cancellationToken);
            //if (bookingDetails.Any())
            //{
            //    throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể thay đổi trạng thái tuyến đường");
            //}
            if (await HasDriver(route.Id, cancellationToken))
            {
                throw new ApplicationException("Tuyến đường đã được Booking! Không thể cập nhật thông tin tuyến đường");
            }

            if (route.Type == RouteType.ROUND_TRIP)
            {
                if (route.RoundTripRouteId.HasValue)
                {
                    Route roundTrip = await work.Routes.GetAsync(route.RoundTripRouteId.Value, cancellationToken: cancellationToken);

                    if (roundTrip.Status != newStatus)
                    {
                        roundTrip.Status = newStatus;
                    }

                    await work.Routes.UpdateAsync(roundTrip);
                }
                else
                {
                    throw new ApplicationException("Đây là tuyến đường về thuộc tuyến đường khứ hồi! " +
                        "Không thể cập nhật trạng thái tuyến đường này. Vui lòng cập nhật trạng thái tuyến đường chính!");
                }
            }

            if (route.Status != newStatus)
            {
                route.Status = newStatus;
            }

            await work.Routes.UpdateAsync(route);
            await work.SaveChangesAsync(cancellationToken);

            return route;
        }

        public async Task<Route> DeleteRouteAsync(Guid routeId,
            CancellationToken cancellationToken)
        {
            Route route = await work.Routes.GetAsync(routeId, cancellationToken: cancellationToken);
            if (route is null)
            {
                throw new ApplicationException("Không tìm thấy tuyến đường được chỉ định!!");
            }

            if (!IdentityUtilities.IsAdmin())
            {
                if (!route.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new ApplicationException("Bạn không thể thực hiện hành động này!!");
                }
            }

            // Check for Booking
            // Not tested yet
            // TODO Code
            //IEnumerable<Booking> bookings = await work.Bookings
            //    .GetAllAsync(query => query.Where(
            //        b => b.CustomerRouteId.Equals(routeId)),
            //        cancellationToken: cancellationToken);
            //IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

            //IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
            //    .GetAllAsync(query => query.Where(
            //        b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
            //        /*|| (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id))*/ // Driver Route
            //        && b.Status != BookingDetailStatus.CANCELLED)),
            //        cancellationToken: cancellationToken);
            //if (bookingDetails.Any())
            //{
            //    throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể xóa tuyến đường");
            //}
            if (await HasDriver(route.Id, cancellationToken))
            {
                throw new ApplicationException("Tuyến đường đã được Booking! Không thể cập nhật thông tin tuyến đường");
            }

            // Booking and Booking Details are deleted as well
            // NOT TESTED yet
            // TODO code
            //IEnumerable<BookingDetail> deleteBookingDetails = await work.BookingDetails
            //    .GetAllAsync(query => query.Where(
            //        b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
            //        /*|| (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id))*/ // Driver Route
            //        )),
            //        cancellationToken: cancellationToken);
            //IEnumerable<Guid> deleteBookingIds = deleteBookingDetails.Select(d => d.BookingId).Distinct();
            //IEnumerable<Guid> deletedBookingDetailIds = deleteBookingDetails.Select(d => d.Id);
            //foreach (BookingDetail bookingDetail in deleteBookingDetails)
            //{
            //    await work.BookingDetails.DeleteAsync(bookingDetail);
            //}
            //foreach (Guid bookingId in bookingIds)
            //{
            //    IEnumerable<BookingDetail> checkBookingDetails = await work.BookingDetails
            //        .GetAllAsync(query => query.Where(
            //            bd => bd.BookingId.Equals(bookingId)
            //            && !deletedBookingDetailIds.Contains(bd.Id)),
            //            cancellationToken: cancellationToken);
            //    if (!checkBookingDetails.Any())
            //    {
            //        await work.Bookings.DeleteAsync(b => b.Id.Equals(bookingId),
            //            cancellationToken: cancellationToken);
            //    }
            //}
            IEnumerable<RouteRoutine> routines = await work.RouteRoutines.GetAllAsync(
                query => query.Where(r => r.RouteId.Equals(route.Id)),
                cancellationToken: cancellationToken);

            if (route.Type == RouteType.ROUND_TRIP)
            {
                if (route.RoundTripRouteId.HasValue)
                {
                    Route roundTrip = await work.Routes.GetAsync(route.RoundTripRouteId.Value, cancellationToken: cancellationToken);

                    routines = routines.Concat(await work.RouteRoutines.GetAllAsync(
                        query => query.Where(r => r.RouteId.Equals(roundTrip.Id)),
                        cancellationToken: cancellationToken));

                    await work.Routes.DeleteAsync(roundTrip, cancellationToken: cancellationToken);

                }
                else
                {
                    throw new ApplicationException("Đây là tuyến đường về thuộc tuyến đường khứ hồi! " +
                        "Không thể xóa tuyến đường này. Vui lòng xóa tuyến đường chính!");
                }
            }

            foreach (RouteRoutine routine in routines)
            {
                await work.RouteRoutines.DeleteAsync(routine, cancellationToken: cancellationToken);
            }

            await work.Routes.DeleteAsync(route, cancellationToken: cancellationToken);

            await work.SaveChangesAsync(cancellationToken);

            return route;
        }

        #region Validation
        private void IsValidStation(StationViewModel station, string stationName)
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
                minLength: 1,
                minLengthErrorMessage: "Tên " + stationName + " phải có từ 1 kí tự trở lên!",
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



        private async Task<Booking?> HasBooking(Guid routeId,
            CancellationToken cancellationToken)
        {
            Route? route = await work.Routes.GetAsync(routeId, cancellationToken: cancellationToken);

            if (route is null)
            {
                throw new ApplicationException("Tuyến đường không tồn tại!!");
            }

            Guid checkRouteId = routeId;
            if (route.Type == RouteType.ROUND_TRIP
                && !route.RoundTripRouteId.HasValue)
            {
                // The roundtrip route
                // Get the main route
                Route mainRoute = await work.Routes.GetAsync(
                    r => r.RoundTripRouteId.HasValue &&
                    r.RoundTripRouteId.Value.Equals(routeId), cancellationToken: cancellationToken);
                checkRouteId = mainRoute.Id;
            }

            Booking? booking = await work.Bookings.GetAsync(
                    b => b.CustomerRouteId.Equals(checkRouteId)
                    && b.Status == BookingStatus.CONFIRMED, cancellationToken: cancellationToken);

            return booking;
            //IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);
            //IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
            //    .GetAllAsync(query => query.Where(
            //        d => bookingIds.Contains(d.Id)), cancellationToken: cancellationToken);

            //return bookingDetails.Any(d => d.DriverId.HasValue);
        }

        private async Task<bool> HasDriver(Guid routeId, CancellationToken cancellationToken)
        {
            //IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
            //    .GetAllAsync(query => query.Where(
            //        d => d.BookingId.Equals(bookingId)),
            //        cancellationToken: cancellationToken);
            //return bookingDetails.Any(d => d.DriverId.HasValue);
            IEnumerable<Booking> bookings = await work.Bookings
                .GetAllAsync(query => query.Where(
                    b => b.CustomerRouteId.Equals(routeId)), cancellationToken: cancellationToken);
            IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);
            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    d => bookingIds.Contains(d.BookingId)),
                    cancellationToken: cancellationToken);
            return bookingDetails.Any(d => d.DriverId.HasValue);
        }
        #endregion
    }
}
