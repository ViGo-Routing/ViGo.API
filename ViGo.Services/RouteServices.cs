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
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class RouteServices : BaseServices
    {
        public RouteServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<Route> CreateRouteAsync(RouteCreateEditModel dto,
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin()
                && !IdentityUtilities.IsStaff())
            {
                if (!dto.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!");
                }
            }

            User user = await work.Users.GetAsync(
                u => u.Id.Equals(dto.UserId) && u.Status == UserStatus.ACTIVE, 
                cancellationToken: cancellationToken);
            if (user is null || (user.Role != UserRole.DRIVER && user.Role != UserRole.CUSTOMER))
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
            
            if (!Enum.IsDefined(dto.RouteType) ||
                (user.Role == UserRole.CUSTOMER &&
                (dto.RouteType != RouteType.SPECIFIC_ROUTE_SPECIFIC_TIME)))
            {
                throw new ApplicationException("Loại Tuyến đường của khách hàng không hợp lệ!!");
            }

            if (dto.RouteType == RouteType.EVERY_ROUTE_SPECIFIC_TIME ||
                dto.RouteType == RouteType.EVERY_ROUTE_EVERY_TIME)
            {
                // No need to set up Start, End
                if (dto.StartStation != null || dto.EndStation != null)
                {
                    throw new ApplicationException("Tuyến đường này không cần thiết lập điểm bắt đầu và kết thúc!");
                }

                if (dto.Distance.HasValue && dto.Distance.Value != 0)
                {
                    throw new ApplicationException("Tuyến đường này không cần thiết thiết lập khoảng cách!");
                }
                if (dto.Duration.HasValue && dto.Duration.Value != 0)
                {
                    throw new ApplicationException("Tuyến đường này không cần thiết thiết lập thời gian di chuyển!");
                }
            }
            else
            {
                if (dto.StartStation == null || dto.EndStation == null)
                {
                    throw new ApplicationException("Tuyến đường chưa được thiết lập điểm bắt đầu và kết thúc!!");
                }

                if (!dto.Distance.HasValue || dto.Distance <= 0)
                {
                    throw new ApplicationException("Khoảng cách tuyến đường phải lớn hơn 0!");
                }
                if (!dto.Duration.HasValue || dto.Distance <= 0)
                {
                    throw new ApplicationException("Thời gian di chuyển của tuyến đường phải lớn hơn 0!");
                }
                IsValidStation(dto.StartStation, "Điểm bắt đầu");
                IsValidStation(dto.EndStation, "Điểm kết thúc");
            }
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

            Station? startStation = null;

            if (dto.StartStation != null)
            {
                // Find Start Station existance
                startStation = await work.Stations.GetAsync(
                s => s.Longtitude == dto.StartStation.Longtitude
                && s.Latitude == dto.StartStation.Latitude
                && s.Status == StationStatus.ACTIVE,
                cancellationToken: cancellationToken);
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
                    await work.Stations.InsertAsync(startStation, cancellationToken: cancellationToken);
                }
            }

            Station? endStation = null;
            if (dto.EndStation != null)
            {
                // Find End Station existance
                endStation = await work.Stations.GetAsync(
                    s => s.Longtitude == dto.EndStation.Longtitude
                    && s.Latitude == dto.EndStation.Latitude
                    && s.Status == StationStatus.ACTIVE,
                    cancellationToken: cancellationToken);
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
                    await work.Stations.InsertAsync(endStation, cancellationToken: cancellationToken);
                }
            }

            // Create Route
            Route route = new Route
            {
                UserId = dto.UserId,
                Name = dto.Name,
                StartStationId = startStation != null ? startStation.Id : null,
                EndStationId = endStation != null ? endStation.Id : null,
                Distance = dto.Distance,
                Duration = dto.Duration,
                RouteType = dto.RouteType,
                RoutineType = dto.RoutineType,
                Status = RouteStatus.ACTIVE
            };
            await work.Routes.InsertAsync(route, cancellationToken: cancellationToken);

            // Create RouteStations
            IList<RouteStation> routeStations = new List<RouteStation>();
            if (startStation != null)
            {
                routeStations.Add(
                    // Start Station
                    new RouteStation
                    {
                        RouteId = route.Id,
                        StationId = startStation.Id,
                        StationIndex = 1,
                        DistanceFromFirstStation = 0,
                        DurationFromFirstStation = 0,
                        Status = RouteStationStatus.ACTIVE
                    });
            }
            if (endStation != null)
            {
                routeStations.Add(
                    // End Station
                    new RouteStation
                    {
                        RouteId = route.Id,
                        StationId = endStation.Id,
                        StationIndex = 2,
                        DistanceFromFirstStation = dto.Distance,
                        DurationFromFirstStation = dto.Duration,
                        Status = RouteStationStatus.ACTIVE
                    });
            }
            await work.RouteStations.InsertAsync(routeStations, cancellationToken: cancellationToken);

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

            routeStations.ToList()
                .ForEach(rs => rs.Station = null);
            route.RouteStations = routeStations;
            //route.RouteRoutines = routeRoutines.OrderBy(r => r.StartDate)
            //    .ThenBy(r => r.StartTime).ToList();

            //route.EndStation.RouteStations = null;
            //route.StartStation.RouteStations = null;

            return route;
        }

        public async Task<IEnumerable<RouteViewModel>> GetRoutesAsync(Guid? userId,
            CancellationToken cancellationToken)
        {
            IEnumerable<Route> routes = new List<Route>();

            if (userId.HasValue)
            {
                routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => r.UserId.Equals(userId)), cancellationToken: cancellationToken);
            } else
            {
                routes = await work.Routes
                    .GetAllAsync(cancellationToken: cancellationToken);
            }

            if (routes.Any())
            {
                IEnumerable<Guid> routeIds = routes.Select(r => r.Id);

                IEnumerable<RouteStation> routeStations = await work.RouteStations
                    .GetAllAsync(query => query.Where(
                        s => routeIds.Contains(s.RouteId)), 
                        cancellationToken: cancellationToken);

                IEnumerable<Guid> stationIds = routeStations.Select(s => s.StationId).Distinct();
                IEnumerable<Station> stations = await work.Stations
                    .GetAllAsync(query => query.Where(
                        s => stationIds.Contains(s.Id)), 
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

            IEnumerable<RouteStation> routeStations = await work.RouteStations
                .GetAllAsync(query => query.Where(
                    s => s.RouteId.Equals(routeId)), cancellationToken: cancellationToken);

            IEnumerable<Guid> stationIds = routeStations.Select(s => s.StationId).Distinct();
            IEnumerable<Station> stations = await work.Stations
                .GetAllAsync(query => query.Where(
                    s => stationIds.Contains(s.Id)), cancellationToken: cancellationToken);

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
            StationViewModel? startStationDto = null;
            if (route.StartStationId != null)
            {
                Station? startStation = stations.SingleOrDefault(
                    s => s.Id.Equals(route.StartStationId));
                startStationDto = new StationViewModel(
                    startStation, 1
                    );
            }

            StationViewModel? endStationDto = null;
            if (route.EndStationId != null)
            {
                Station? endStation = stations.SingleOrDefault(
                s => s.Id.Equals(route.EndStationId));
                endStationDto = new StationViewModel(
                    endStation, 2
                    );
            }

            // Route Stations
            IEnumerable<RouteStationViewModel> routeStationDtos =
                (from routeStation in routeStations
                 join station in stations
                    on routeStation.StationId equals station.Id
                 select new RouteStationViewModel(routeStation, station))
                 .OrderBy(s => s.StationIndex);

            // User
            User user = await work.Users.GetAsync(route.UserId, cancellationToken: cancellationToken);
            UserViewModel userViewModel = new UserViewModel(user);
            return new RouteViewModel(
                    route,
                    startStationDto,
                    endStationDto,
                    userViewModel);

            //RouteViewModel model = new RouteViewModel(route);
            //return model;

        }

        public async Task<Route> UpdateRouteAsync(RouteCreateEditModel updateDto,
            CancellationToken cancellationToken)
        {
            if (!updateDto.Id.HasValue)
            {
                throw new ApplicationException("Thông tin tuyến đường không hợp lệ!!");
            }

            Route route = await work.Routes.GetAsync(updateDto.Id.Value, cancellationToken: cancellationToken);
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
            IEnumerable<Booking> bookings = await work.Bookings
                .GetAllAsync(query => query.Where(
                    b => b.CustomerRouteId.Equals(updateDto.Id)),
                    cancellationToken: cancellationToken);
            IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
                    || (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id)) // Driver Route
                    && b.Status != BookingDetailStatus.CANCELLED)),
                    cancellationToken: cancellationToken);
            if (bookingDetails.Any())
            {
                throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể cập nhật thông tin tuyến đường");
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

            if (updateDto.Distance.HasValue 
                && updateDto.Distance.Value <= 0)
            {
                throw new ApplicationException("Khoảng cách tuyến đường phải lớn hơn 0!");
            } else
            {
                route.Distance = updateDto.Distance;
            }
            if (updateDto.Duration.HasValue &&
                updateDto.Duration.Value <= 0)
            {
                throw new ApplicationException("Thời gian di chuyển của tuyến đường phải lớn hơn 0!");
            } else
            {
                route.Duration = updateDto.Duration;
            }

            IList<RouteStation> routeStations = new List<RouteStation>();
            Station? startStation = null, endStation = null;

            if (updateDto.RouteType == RouteType.EVERY_ROUTE_SPECIFIC_TIME ||
                updateDto.RouteType == RouteType.EVERY_ROUTE_EVERY_TIME)
            {
                // No need to set up Start, End
                if (updateDto.StartStation != null || updateDto.EndStation != null)
                {
                    throw new ApplicationException("Tuyến đường này không cần thiết lập điểm bắt đầu và kết thúc!");
                }
            }
            else
            {
                //if (updateDto.StartStation == null || updateDto.EndStation == null)
                //{
                //    throw new ApplicationException("Tuyến đường chưa được thiết lập điểm bắt đầu và kết thúc!!");
                //}
                //IsValidStation(updateDto.StartStation, "Điểm bắt đầu");
                //IsValidStation(updateDto.EndStation, "Điểm kết thúc");

                if (updateDto.StartStation != null)
                {
                    IsValidStation(updateDto.StartStation, "Điểm bắt đầu");

                    // Find Start Station existance
                    startStation = await work.Stations.GetAsync(
                        s => s.Longtitude == updateDto.StartStation.Longtitude
                        && s.Latitude == updateDto.StartStation.Latitude
                        && s.Status == StationStatus.ACTIVE,
                        cancellationToken: cancellationToken);
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
                        await work.Stations.InsertAsync(startStation, cancellationToken: cancellationToken);
                    }

                    routeStations.Add(
                        // Start Station
                        new RouteStation
                        {
                            RouteId = route.Id,
                            StationId = startStation.Id,
                            StationIndex = 1,
                            DistanceFromFirstStation = 0,
                            DurationFromFirstStation = 0,
                            Status = RouteStationStatus.ACTIVE
                        }
                    );
                    route.StartStationId = startStation.Id;
                }

                if (updateDto.EndStation != null)
                {
                    IsValidStation(updateDto.EndStation, "Điểm kết thúc");

                    // Find End Station existance
                    endStation = await work.Stations.GetAsync(
                        s => s.Longtitude == updateDto.EndStation.Longtitude
                        && s.Latitude == updateDto.EndStation.Latitude
                        && s.Status == StationStatus.ACTIVE,
                        cancellationToken: cancellationToken);
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
                        await work.Stations.InsertAsync(endStation, cancellationToken: cancellationToken);
                    }

                    routeStations.Add(
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
                    );
                    route.EndStationId = endStation.Id;
                }
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

            // Update Route
            await work.Routes.UpdateAsync(route);

            // Update RouteStations
            if (routeStations.Count > 0)
            {
                IEnumerable<RouteStation> currentRouteStations
                = await work.RouteStations.GetAllAsync(query => query.Where(
                    s => s.RouteId.Equals(route.Id)), cancellationToken: cancellationToken);
                foreach (RouteStation routeStation in currentRouteStations)
                {
                    await work.RouteStations.DeleteAsync(routeStation, isSoftDelete: false);
                }

                await work.RouteStations.InsertAsync(routeStations, cancellationToken: cancellationToken);
            }

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

            await work.SaveChangesAsync(cancellationToken);

            routeStations.ToList()
                .ForEach(rs => rs.Station = null);
            route.RouteStations = routeStations;
            //route.RouteRoutines = routeRoutines.OrderBy(r => r.StartDate)
            //    .ThenBy(r => r.StartTime).ToList();
            route.EndStation = endStation;
            route.StartStation = startStation;

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
            if (route == null)
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
            IEnumerable<Booking> bookings = await work.Bookings
                .GetAllAsync(query => query.Where(
                    b => b.CustomerRouteId.Equals(routeId)),
                    cancellationToken: cancellationToken);
            IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
                    || (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id)) // Driver Route
                    && b.Status != BookingDetailStatus.CANCELLED)), 
                    cancellationToken: cancellationToken);
            if (bookingDetails.Any())
            {
                throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể thay đổi trạng thái tuyến đường");
            }

            if (route.Status != newStatus)
            {
                route.Status = newStatus;
                await work.Routes.UpdateAsync(route);
                await work.SaveChangesAsync(cancellationToken);
            }

            return route;
        }

        public async Task<Route> DeleteRouteAsync(Guid routeId, 
            CancellationToken cancellationToken)
        {
            Route route = await work.Routes.GetAsync(routeId, cancellationToken: cancellationToken);
            if (route == null)
            {
                throw new ApplicationException("Không tìm thấy tuyến đường được chỉ định!!");
            }

            // Check for Booking
            // Not tested yet
            // TODO Code
            IEnumerable<Booking> bookings = await work.Bookings
                .GetAllAsync(query => query.Where(
                    b => b.CustomerRouteId.Equals(routeId)),
                    cancellationToken: cancellationToken);
            IEnumerable<Guid> bookingIds = bookings.Select(b => b.Id);

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
                    || (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id)) // Driver Route
                    && b.Status != BookingDetailStatus.CANCELLED)),
                    cancellationToken: cancellationToken);
            if (bookingDetails.Any())
            {
                throw new ApplicationException("Tuyến đường đã được xếp lịch di chuyển cho tài xế! Không thể xóa tuyến đường");
            }

            // Booking and Booking Details are deleted as well
            // NOT TESTED yet
            // TODO code
            IEnumerable<BookingDetail> deleteBookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    b => ((bookingIds.Contains(b.Id) && b.DriverId.HasValue) // Customer Route and a driver has been assigned
                    || (b.DriverRouteId.HasValue && b.DriverRouteId.Equals(route.Id)) // Driver Route
                    )),
                    cancellationToken: cancellationToken);
            IEnumerable<Guid> deleteBookingIds = deleteBookingDetails.Select(d => d.BookingId).Distinct();
            IEnumerable<Guid> deletedBookingDetailIds = deleteBookingDetails.Select(d => d.Id);
            foreach (BookingDetail bookingDetail in deleteBookingDetails)
            {
                await work.BookingDetails.DeleteAsync(bookingDetail);
            }
            foreach (Guid bookingId in bookingIds)
            {
                IEnumerable<BookingDetail> checkBookingDetails = await work.BookingDetails
                    .GetAllAsync(query => query.Where(
                        bd => bd.BookingId.Equals(bookingId)
                        && !deletedBookingDetailIds.Contains(bd.Id)), 
                        cancellationToken: cancellationToken);
                if (!checkBookingDetails.Any())
                {
                    await work.Bookings.DeleteAsync(b => b.Id.Equals(bookingId), 
                        cancellationToken: cancellationToken);
                }
            }

            await work.Routes.DeleteAsync(route);

            await work.SaveChangesAsync(cancellationToken);

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
        #endregion
    }
}
