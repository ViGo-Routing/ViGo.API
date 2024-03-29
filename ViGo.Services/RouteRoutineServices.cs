﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.GoogleMaps;
using ViGo.Models.QueryString.Pagination;
using ViGo.Models.RouteRoutines;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Google;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class RouteRoutineServices : BaseServices
    {
        public RouteRoutineServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IEnumerable<RouteRoutineViewModel>>
            GetRouteRoutinesAsync(Guid routeId,
            PaginationParameter pagination,
            HttpContext context, CancellationToken cancellationToken)
        {
            IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(routeId)), cancellationToken: cancellationToken);

            routeRoutines = routeRoutines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.PickupTime);

            //int totalRecords = routeRoutines.Count();

            //routeRoutines = routeRoutines.ToPagedEnumerable(
            //    pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<RouteRoutineViewModel> models =
                from routeRoutine in routeRoutines
                select new RouteRoutineViewModel(routeRoutine);
            models = models.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.PickupTime);

            //return models.ToPagedEnumerable(
            //    pagination.PageNumber, pagination.PageSize, totalRecords, context);
            return models;
        }

        public async Task<RouteRoutineViewModel> GetRouteRoutineAsync(Guid routineId, CancellationToken cancellationToken)
        {
            RouteRoutine? routeRoutine = await work.RouteRoutines.GetAsync(routineId,
                cancellationToken: cancellationToken);

            if (routeRoutine is null)
            {
                throw new ApplicationException("Thông tin lịch trình không tồn tại!!");
            }

            RouteRoutineViewModel routineViewModel = new RouteRoutineViewModel(routeRoutine);
            return routineViewModel;
        }

        public async Task<IEnumerable<RouteRoutine>> CreateRouteRoutinesAsync(RouteRoutineCreateModel model,
            CancellationToken cancellationToken)
        {
            Route? route = await work.Routes.GetAsync(model.RouteId, cancellationToken: cancellationToken);
            if (route == null)
            {
                throw new ApplicationException("Tuyến đường không tồn tại!!");
            }

            if (model.RouteRoutines.Count == 0)
            {
                throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            }

            if (!IdentityUtilities.IsAdmin()
                && !IdentityUtilities.IsStaff())
            {
                if (!route.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!");
                }
            }

            IEnumerable<RouteRoutine> currentRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(model.RouteId)), cancellationToken: cancellationToken);
            if (currentRoutines.Any() && (await HasBooking(model.RouteId, cancellationToken) != null))
            {
                throw new ApplicationException("Tuyến đường này đã được thiết lập lịch trình từ trước! " +
                    "Vui lòng dùng tính năng cập nhật lịch trình!");
            }

            foreach (RouteRoutineListItemModel routine in model.RouteRoutines)
            {
             await   IsValidRoutine(routine, cancellationToken);
            }

            await IsValidRoutines(model.RouteRoutines, route, cancellationToken: cancellationToken);

            if (currentRoutines.Any())
            {
                currentRoutines = currentRoutines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.PickupTime);
                //IList<RouteRoutineListItemModel> newRoutines = new List<RouteRoutineListItemModel>();

                IList<Guid> routineToDelete = new List<Guid>();
                foreach (RouteRoutine oldRoutine in currentRoutines)
                {
                    RouteRoutineListItemModel routineModel = new RouteRoutineListItemModel(oldRoutine);
                    if (!model.RouteRoutines.Contains(routineModel))
                    {
                        // New Routine does not exist in the database
                        // Need to check for validity and need to delete
                        //newRoutines.Add(routineModel);

                        await work.RouteRoutines.DetachAsync(oldRoutine);

                        //routineToDelete.Add(oldRoutine.Id);
                        await work.RouteRoutines.DeleteAsync(oldRoutine, isSoftDelete: true, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        model.RouteRoutines.Remove(routineModel);
                    }
                }
            }

            IList<RouteRoutine> routeRoutines =
                (from routine in model.RouteRoutines
                 select new RouteRoutine
                 {
                     RouteId = model.RouteId,
                     RoutineDate = routine.RoutineDate.ToDateTime(TimeOnly.MinValue),
                     PickupTime = routine.PickupTime.ToTimeSpan(),
                     Status = RouteRoutineStatus.ACTIVE
                 }).ToList();

            await work.RouteRoutines.InsertAsync(routeRoutines, cancellationToken: cancellationToken);
            await work.SaveChangesAsync(cancellationToken);
            routeRoutines = routeRoutines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.PickupTime).ToList();

            return routeRoutines;
        }

        public async Task<IEnumerable<RouteRoutine>> UpdateRouteRoutinesAsync(RouteRoutineUpdateModel model,
            bool checkForRoundTripRoutines = true,
            CancellationToken cancellationToken = default)
        {
            Route? route = await work.Routes.GetAsync(model.RouteId, cancellationToken: cancellationToken);
            if (route == null)
            {
                throw new ApplicationException("Tuyến đường không tồn tại!!");
            }

            //if (route.RouteType == RouteType.EVERY_ROUTE_EVERY_TIME
            //    && model.RouteRoutines.Count > 0)
            //{
            //    throw new ApplicationException("Tuyến đường này không cần thiết lập lịch trình đi!!");
            //}

            if (model.RouteRoutines.Count == 0)
            {
                throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            }

            if (!IdentityUtilities.IsAdmin()
                && !IdentityUtilities.IsStaff())
            {
                //if (!route.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                //{
                //    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!");
                //}
            }

            if (await HasDriver(route.Id, cancellationToken))
            {
                throw new ApplicationException("Hành trình đã có tài xế chọn nên không thể cập nhật thông tin!");
            }

            foreach (RouteRoutineListItemModel routine in model.RouteRoutines)
            {
                await IsValidRoutine(routine, cancellationToken);
            }

            // Check for existing Routines
            IEnumerable<RouteRoutine> currentRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(model.RouteId)), cancellationToken: cancellationToken);

            if (checkForRoundTripRoutines)
            {
                if (!currentRoutines.Any())
                {
                    throw new ApplicationException("Tuyến đường này chưa được thiết lập lịch trình!! " +
                    "Vui lòng dùng chức năng Tạo lịch trình!");
                }
            }

            if (currentRoutines.Any())
            {
                currentRoutines = currentRoutines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.PickupTime);
                //IList<RouteRoutineListItemModel> newRoutines = new List<RouteRoutineListItemModel>();

                IList<Guid> routineToDelete = new List<Guid>();
                foreach (RouteRoutine oldRoutine in currentRoutines)
                {
                    RouteRoutineListItemModel routineModel = new RouteRoutineListItemModel(oldRoutine);
                    if (!model.RouteRoutines.Contains(routineModel))
                    {
                        // New Routine does not exist in the database
                        // Need to check for validity and need to delete
                        //newRoutines.Add(routineModel);

                        await work.RouteRoutines.DetachAsync(oldRoutine);

                        //routineToDelete.Add(oldRoutine.Id);
                        await work.RouteRoutines.DeleteAsync(oldRoutine, isSoftDelete: true, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        model.RouteRoutines.Remove(routineModel);
                    }
                }
            }


            //if (routineToDelete.Count > 0)
            //{

            //}

            IList<RouteRoutine> routeRoutines = new List<RouteRoutine>();
            IEnumerable<RouteRoutineListItemModel> routinesModel = model.RouteRoutines;
            if (routinesModel.Any())
            {
                await IsValidRoutines(routinesModel.ToList(), route,
                    checkForRoundTripRoutines: checkForRoundTripRoutines, isUpdate: true, null, null, cancellationToken);
                routeRoutines =
                    (from routine in routinesModel
                     select new RouteRoutine
                     {
                         RouteId = model.RouteId,
                         RoutineDate = routine.RoutineDate.ToDateTime(TimeOnly.MinValue),
                         PickupTime = routine.PickupTime.ToTimeSpan(),
                         //EndTime = routine.EndTime.ToTimeSpan(),
                         Status = RouteRoutineStatus.ACTIVE
                     }).ToList();
                await work.RouteRoutines.InsertAsync(routeRoutines, cancellationToken: cancellationToken);
            }

            await work.SaveChangesAsync(cancellationToken);
            IEnumerable<RouteRoutine> updatedRouteRoutines = (await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(route.Id)), cancellationToken: cancellationToken))
                    .OrderBy(r => r.RoutineDate)
                    .ThenBy(r => r.PickupTime);

            return updatedRouteRoutines;
        }

        public async Task<RouteRoutine> UpdateRouteRoutineAsync(RouteRoutineSingleUpdateModel model,
            CancellationToken cancellationToken)
        {
            RouteRoutine? routeRoutine = await work.RouteRoutines.GetAsync(model.Id,
                 cancellationToken: cancellationToken);

            if (routeRoutine is null)
            {
                throw new ApplicationException("Thông tin lịch trình không tồn tại!!");
            }

            if (!Enum.IsDefined(model.Status))
            {
                throw new ApplicationException("Trạng thái lịch trình không hợp lý!!");
            }

            RouteRoutineListItemModel itemModel = new RouteRoutineListItemModel()
            {
                RoutineDate = model.RoutineDate,
                PickupTime = model.PickupTime,
                Status = model.Status
            };

            await IsValidRoutine(itemModel, cancellationToken);

            Route route = await work.Routes.GetAsync(routeRoutine.RouteId, cancellationToken: cancellationToken);
            IEnumerable<RouteRoutine> currentRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(r => r.RouteId.Equals(routeRoutine.RouteId)
                                    && !r.Id.Equals(model.Id)), // Routines that are not this one
                    cancellationToken: cancellationToken);

            IEnumerable<RouteRoutineListItemModel> routinesModel = from routine in currentRoutines
                                                                   select new RouteRoutineListItemModel(routine);
            routinesModel = routinesModel.Append(itemModel);
            await IsValidRoutines(routinesModel.ToList(), route,
                 isUpdate: true,
                 cancellationToken: cancellationToken);

            routeRoutine.RoutineDate = model.RoutineDate.ToDateTime(TimeOnly.MinValue);
            routeRoutine.PickupTime = model.PickupTime.ToTimeSpan();
            routeRoutine.Status = model.Status;

            await work.RouteRoutines.UpdateAsync(routeRoutine);
            await work.SaveChangesAsync(cancellationToken);

            return routeRoutine;
        }

        public async Task<RouteRoutine> DeleteRouteRoutineAsync(Guid routineId,
            CancellationToken cancellationToken)
        {
            RouteRoutine? routeRoutine = await work.RouteRoutines.GetAsync(routineId,
                cancellationToken: cancellationToken);
            if (routeRoutine is null)
            {
                throw new ApplicationException("Lịch trình không tồn tại!!");
            }

            Route route = await work.Routes.GetAsync(routeRoutine.RouteId,
                cancellationToken: cancellationToken);

            if (!IdentityUtilities.IsAdmin())
            {
                if (!route.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new ApplicationException("Bạn không thể thực hiện hành động này!!");
                }
            }

            IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(b => b.CustomerRouteRoutineId.Equals(routineId)),
                cancellationToken: cancellationToken);
            if (bookingDetails.Any())
            {
                throw new ApplicationException("Lịch trình đã được Booking nên không thể xóa!");
            }

            await work.RouteRoutines.DeleteAsync(routeRoutine, cancellationToken: cancellationToken);

            if (route.Type == RouteType.ROUND_TRIP)
            {
                RouteRoutine? roundTripRoutine = null;
                if (route.RoundTripRouteId.HasValue)
                {
                    // route is the MainRoute
                    Route roundTripRoute = await work.Routes.GetAsync(
                        route.RoundTripRouteId.Value, cancellationToken: cancellationToken);

                    roundTripRoutine = await work.RouteRoutines
                        .GetAsync(r => r.RouteId.Equals(roundTripRoute.Id)
                            && r.RoutineDate.Date.Equals(routeRoutine.RoutineDate.Date),
                            cancellationToken: cancellationToken);

                }
                else
                {
                    // route is the roundtrip Route
                    Route mainRoute = await work.Routes.GetAsync(
                        r => r.RoundTripRouteId.HasValue &&
                        r.RoundTripRouteId.Value.Equals(route.Id),
                        cancellationToken: cancellationToken);

                    roundTripRoutine = await work.RouteRoutines
                        .GetAsync(r => r.RouteId.Equals(mainRoute.Id)
                            && r.RoutineDate.Date.Equals(routeRoutine.RoutineDate.Date),
                            cancellationToken: cancellationToken);

                }

                if (roundTripRoutine != null)
                {
                    await work.RouteRoutines.DeleteAsync(roundTripRoutine,
                        cancellationToken: cancellationToken);
                }
            }

            await work.SaveChangesAsync(cancellationToken);

            return routeRoutine;
        }

        public async Task CheckRouteRoutinesAsync(RouteRoutineCheckModel checkModel,
            CancellationToken cancellationToken)
        {
            Route? route = await work.Routes.GetAsync(checkModel.RouteId,
                cancellationToken: cancellationToken);
            if (route == null)
            {
                throw new ApplicationException("Tuyến đường không tồn tại!!");
            }

            if (checkModel.RouteRoutines.Count == 0)
            {
                throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            }

            foreach (RouteRoutineListItemModel routine in checkModel.RouteRoutines)
            {
                await IsValidRoutine(routine, cancellationToken);
            }

            // Check for existing Routines
            IEnumerable<RouteRoutine> currentRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(checkModel.RouteId)), cancellationToken: cancellationToken);

            bool isUpdate = false;
            if (currentRoutines.Any())
            {
                //Đã có lịch trình
                isUpdate = true;
            }
            //currentRoutines = currentRoutines.OrderBy(r => r.RoutineDate)
            //    .ThenBy(r => r.PickupTime);

            IList<RouteRoutine> routeRoutines = new List<RouteRoutine>();
            IEnumerable<RouteRoutineListItemModel> routinesModel = checkModel.RouteRoutines;

            if (routinesModel.Any())
            {
                await IsValidRoutines(routinesModel.ToList(), route,
                    checkForRoundTripRoutines: true, isUpdate: isUpdate, startPoint: checkModel.StartPoint,
                    endPoint: checkModel.EndPoint, cancellationToken: cancellationToken);
            }

        }

        public async Task CheckRoundRouteRoutinesAsync(RoundRouteRoutineCheckModel checkModel,
            CancellationToken cancellationToken)
        {
            Route? route = await work.Routes.GetAsync(checkModel.RouteId,
                cancellationToken: cancellationToken);
            if (route == null)
            {
                throw new ApplicationException("Tuyến đường không tồn tại!!");
            }

            if (route.Type != RouteType.ROUND_TRIP && (checkModel.StartPoint == null || checkModel.EndPoint == null))
            {
                throw new ApplicationException("Loại tuyến đường không phù hợp!!");
            }

            if (!route.RoundTripRouteId.HasValue && (checkModel.StartPoint == null || checkModel.EndPoint == null))
            {
                // Not the main route
                throw new ApplicationException("Thông tin tuyến đường không hợp lệ!!");
            }

            if (checkModel.RouteRoutines.Count == 0 || checkModel.RoundRouteRoutines.Count == 0)
            {
                throw new ApplicationException("Lịch trình đi chưa được thiết lập!!");
            }

            //foreach (RouteRoutineListItemModel routine in checkModel.RouteRoutines)
            //{
            //    IsValidRoutine(routine);
            //}
            //foreach (RouteRoutineListItemModel routine in checkModel.RoundRouteRoutines)
            //{
            //    IsValidRoutine(routine);
            //}
           

            IEnumerable<RouteRoutineListItemModel> routines = checkModel.RouteRoutines;
            IEnumerable<RouteRoutineListItemModel> roundTripRoutines = checkModel.RoundRouteRoutines;
            // RoundTrip rountines have been configured
            if (roundTripRoutines.Count() != routines.Count())
            {
                throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
                    $"Tuyến đường chiều về có tổng cộng {roundTripRoutines.Count()} lịch trình, trong khi lịch trình " +
                    $"cho tuyến đường chiều đi sắp được thiết lập có {routines.Count()} lịch trình!");
            }

            double travelTime = 0;

            if (checkModel.StartPoint != null && checkModel.EndPoint != null)
            {
                travelTime = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
                   checkModel.EndPoint,
                   checkModel.StartPoint,
                   cancellationToken);
            }
            else
            {
                Route roundTripRoute = await work.Routes.GetAsync(
               route.RoundTripRouteId.Value, cancellationToken: cancellationToken);

                Station mainTripEndStation = await work.Stations
                  .GetAsync(route.EndStationId, cancellationToken: cancellationToken);
                Station roundTripStartStation = await work.Stations
                    .GetAsync(roundTripRoute.StartStationId, cancellationToken: cancellationToken);

                travelTime = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
                    new Models.GoogleMaps.GoogleMapPoint
                    {
                        Latitude = mainTripEndStation.Latitude,
                        Longitude = mainTripEndStation.Longitude
                    },
                    new Models.GoogleMaps.GoogleMapPoint
                    {
                        Latitude = roundTripStartStation.Latitude,
                        Longitude = roundTripStartStation.Longitude
                    },
                    cancellationToken);
            }


            foreach (RouteRoutineListItemModel roundTripRoutine in roundTripRoutines)
            {
                DateOnly routineDate = roundTripRoutine.RoutineDate;

                var routine = routines.SingleOrDefault(r => r.RoutineDate.Equals(routineDate
                    ));

                if (routine is null)
                {
                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
                        $"Tuyến đường chiều về có lịch trình cho ngày {routineDate} nhưng không tìm thấy " +
                        $"lịch trình cho tuyến đường chiều đi cho ngày này!!");
                }

                DateTime pickupDateTime = routineDate.ToDateTime(routine.PickupTime);
                DateTime roundTripPickupDateTime = routineDate
                    .ToDateTime(roundTripRoutine.PickupTime)/*.AddMinutes(30)*/;



                //if (pickupDateTime > roundTripPickupDateTime)
                //{
                //    // Newly setup pickup time is later than the roundtrip pickup + 30 minutes
                //    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
                //        $"Tuyến đường chiều về có lịch trình cho ngày {routineDate} vào lúc " +
                //        $"{TimeOnly.FromTimeSpan(roundTripRoutine.PickupTime)} nhưng lịch trình cho tuyến đường chiều đi cho ngày này " +
                //        $"lại được xếp trễ hơn quá 30 phút ({routine.PickupTime})!!");
                //}
                if ((roundTripPickupDateTime - pickupDateTime).TotalMinutes <= travelTime + 10)
                {
                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
                        $"Thời gian di chuyến giữa chuyến đi và về ước tính {Math.Round(travelTime, 0) + 10} phút, vui lòng " +
                        $"thiết lập lịch trình với thời gian đón phù hợp!");
                }
            }

        }

        #region Validation

        private async Task IsValidRoutine(RouteRoutineListItemModel routine,
            CancellationToken cancellationToken)
        {
            DateTime startDateTime = DateTimeUtilities
            .ToDateTime(routine.RoutineDate, routine.PickupTime);
            //DateTime endDateTime = DateTimeUtilities.
            //    ToDateTime(routine.RoutineDate, routine.StartTime.AddMinutes(30));

            Setting bookedSetting = await work.Settings.GetAsync(
                        s => s.Key.Equals(SettingKeys.TripMustBeBookedBefore_Key),
                        cancellationToken: cancellationToken);

            double maxBookTime = double.Parse(bookedSetting.Value);

            DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();
            startDateTime.DateTimeValidate(
                //minimum: vnNow,
                minimum: vnNow.AddHours(maxBookTime),
                //minErrorMessage: $"Thời gian bắt đầu lịch trình ở quá khứ (ngày: " +
                minErrorMessage: $"Thời gian bắt đầu lịch trình phải trước ít nhất {maxBookTime} tiếng (ngày: " +
                $"{routine.RoutineDate.ToShortDateString()}, " +
                $"giờ: {routine.PickupTime.ToShortTimeString()})"
                //maximum: endDateTime,
                //maxErrorMessage: $"Thời gian kết thúc lịch trình không hợp lệ (ngày: " +
                //$"{routine.RoutineDate.ToShortDateString()}, " +
                //$"giờ: {routine.EndTime.ToShortTimeString()})"
                );
        }

        private async Task IsValidRoutines(IList<RouteRoutineListItemModel> routines,
            Route route, bool checkForRoundTripRoutines = true,
            bool isUpdate = false, GoogleMapPoint? startPoint = null,
            GoogleMapPoint? endPoint = null,
            CancellationToken cancellationToken = default)
        {
            if (route == null)
            {
                throw new Exception("Thiếu thông tin tuyến đường! Vui lòng kiểm tra lại");
            }

            //if (route.RouteType == RouteType.SPECIFIC_ROUTE_SPECIFIC_TIME ||
            //    route.RouteType == RouteType.EVERY_ROUTE_SPECIFIC_TIME)
            //{
            routines = routines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.PickupTime).ToList();

            var groupedRoutines = routines.GroupBy(r => r.RoutineDate);
            foreach (var groupedRoutine in groupedRoutines)
            {
                if (groupedRoutine.Count() > 1)
                {
                    throw new ApplicationException($"Không thể lưu 2 lịch trình cho cùng 1 ngày ({groupedRoutine.Key})!");
                }
            }

            double checkTravelTime = 0;
            if (startPoint != null && endPoint != null)
            {
                checkTravelTime = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
                   startPoint,
                   endPoint,
                   cancellationToken);
            }
            else
            {
                Station startStation = await work.Stations
                   .GetAsync(route.StartStationId, cancellationToken: cancellationToken);
                Station endStation = await work.Stations
                    .GetAsync(route.EndStationId, cancellationToken: cancellationToken);

                checkTravelTime = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
                    new Models.GoogleMaps.GoogleMapPoint
                    {
                        Latitude = startStation.Latitude,
                        Longitude = startStation.Longitude
                    },
                    new Models.GoogleMaps.GoogleMapPoint
                    {
                        Latitude = endStation.Latitude,
                        Longitude = endStation.Longitude
                    },
                    cancellationToken);
            }
            IList<DateTimeRange> routineRanges = new List<DateTimeRange>();

            foreach (RouteRoutineListItemModel routine in routines)
            {
                routineRanges.Add(new DateTimeRange(
                 DateTimeUtilities
                 .ToDateTime(routine.RoutineDate, routine.PickupTime),
                     DateTimeUtilities.
                 ToDateTime(routine.RoutineDate, routine.PickupTime).AddMinutes(checkTravelTime + 10)
                 ));
            }

            //for (int i = 0; i < routineRanges.Count - 1; i++)
            //{
            //    DateTimeRange first = routineRanges[i];
            //    DateTimeRange second = routineRanges[i + 1];

            //    first.IsOverlap(second,
            //        $"Hai khoảng thời gian lịch trình phải cách nhau ít nhất 30 phút" +
            //        $"({first.StartDateTime} và " +
            //        $"{second.StartDateTime})");
            //}

            // Check for RoundTrip
            //if (!isUpdate && checkForRoundTripRoutines && route.Type == RouteType.ROUND_TRIP)
            //{
            //    if (route.RoundTripRouteId.HasValue)
            //    {
            //        // The main route

            //        // Get The roundtrip route
            //        Route roundTripRoute = await work.Routes.GetAsync(route.RoundTripRouteId.Value,
            //            cancellationToken: cancellationToken);
            //        IEnumerable<RouteRoutine> roundTripRoutines = (await work.RouteRoutines
            //            .GetAllAsync(query => query.Where(
            //                r => r.RouteId.Equals(roundTripRoute.Id)), cancellationToken: cancellationToken))
            //                .OrderBy(r => r.RoutineDate);

            //        Station mainTripEndStation = await work.Stations
            //            .GetAsync(route.EndStationId, cancellationToken: cancellationToken);
            //        Station roundTripStartStation = await work.Stations
            //            .GetAsync(roundTripRoute.StartStationId, cancellationToken: cancellationToken);

            //        double travelTime = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
            //            new Models.GoogleMaps.GoogleMapPoint
            //            {
            //                Latitude = mainTripEndStation.Latitude,
            //                Longitude = mainTripEndStation.Longitude
            //            },
            //            new Models.GoogleMaps.GoogleMapPoint
            //            {
            //                Latitude = roundTripStartStation.Latitude,
            //                Longitude = roundTripStartStation.Longitude
            //            },
            //            cancellationToken);

            //        if (roundTripRoutines.Any())
            //        {
            //            // RoundTrip rountines have been configured
            //            if (roundTripRoutines.Count() != routines.Count)
            //            {
            //                throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                    $"Tuyến đường chiều về có tổng cộng {roundTripRoutines.Count()} lịch trình, trong khi lịch trình " +
            //                    $"cho tuyến đường chiều đi sắp được thiết lập có {routines.Count} lịch trình!");
            //            }

            //            foreach (RouteRoutine roundTripRoutine in roundTripRoutines)
            //            {
            //                DateOnly routineDate = DateOnly.FromDateTime(roundTripRoutine.RoutineDate);
            //                var routine = routines.SingleOrDefault(r => r.RoutineDate.Equals(routineDate
            //                    ));

            //                if (routine is null)
            //                {
            //                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                        $"Tuyến đường chiều về có lịch trình cho ngày {routineDate} nhưng không tìm thấy " +
            //                        $"lịch trình cho tuyến đường chiều đi cho ngày này!!");
            //                }
            //                DateTime pickupDateTime = routineDate.ToDateTime(routine.PickupTime);
            //                DateTime roundTripPickupDateTime = routineDate
            //                    .ToDateTime(TimeOnly.FromTimeSpan(roundTripRoutine.PickupTime))/*.AddMinutes(30)*/;

            //                //if (pickupDateTime > roundTripPickupDateTime)
            //                //{
            //                //    // Newly setup pickup time is later than the roundtrip pickup + 30 minutes
            //                //    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                //        $"Tuyến đường chiều về có lịch trình cho ngày {routineDate} vào lúc " +
            //                //        $"{TimeOnly.FromTimeSpan(roundTripRoutine.PickupTime)} nhưng lịch trình cho tuyến đường chiều đi cho ngày này " +
            //                //        $"lại được xếp trễ hơn quá 30 phút ({routine.PickupTime})!!");
            //                //}
            //                if ((roundTripPickupDateTime - pickupDateTime).TotalMinutes <= travelTime + 10)
            //                {
            //                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                        $"Thời gian di chuyến giữa chuyến đi và về ước tính {Math.Round(travelTime, 0) + 10} phút, vui lòng " +
            //                        $"thiết lập lịch trình với thời gian đón phù hợp!");
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {
            //        // The RoundTrip route

            //        // Get the main route
            //        Route mainRoute = await work.Routes.GetAsync(r => r.RoundTripRouteId.HasValue &&
            //            r.RoundTripRouteId.Value.Equals(route.Id),
            //            cancellationToken: cancellationToken);
            //        IEnumerable<RouteRoutine> mainRouteRoutines = (await work.RouteRoutines
            //            .GetAllAsync(query => query.Where(
            //                r => r.RouteId.Equals(mainRoute.Id)), cancellationToken: cancellationToken))
            //                .OrderBy(r => r.RoutineDate);

            //        Station mainTripEndStation = await work.Stations
            //            .GetAsync(mainRoute.EndStationId, cancellationToken: cancellationToken);
            //        Station roundTripStartStation = await work.Stations
            //            .GetAsync(route.StartStationId, cancellationToken: cancellationToken);

            //        double travelTime = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
            //            new Models.GoogleMaps.GoogleMapPoint
            //            {
            //                Latitude = mainTripEndStation.Latitude,
            //                Longitude = mainTripEndStation.Longitude
            //            },
            //            new Models.GoogleMaps.GoogleMapPoint
            //            {
            //                Latitude = roundTripStartStation.Latitude,
            //                Longitude = roundTripStartStation.Longitude
            //            },
            //            cancellationToken);

            //        if (mainRouteRoutines.Any())
            //        {
            //            // Main Route rountines have been configured
            //            if (mainRouteRoutines.Count() != routines.Count)
            //            {
            //                throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                    $"Tuyến đường chiều đi có tổng cộng {mainRouteRoutines.Count()} lịch trình, trong khi lịch trình " +
            //                    $"cho tuyến đường chiều về sắp được thiết lập có {routines.Count} lịch trình!");
            //            }

            //            foreach (RouteRoutine mainRouteRoutine in mainRouteRoutines)
            //            {
            //                DateOnly routineDate = DateOnly.FromDateTime(mainRouteRoutine.RoutineDate);
            //                var routine = routines.SingleOrDefault(r => r.RoutineDate.Equals(routineDate
            //                    ));
            //                if (routine is null)
            //                {
            //                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                        $"Tuyến đường chiều đi có lịch trình cho ngày {routineDate} nhưng không tìm thấy " +
            //                        $"lịch trình cho tuyến đường chiều về cho ngày này!!");
            //                }

            //                DateTime pickupDateTime = routineDate.ToDateTime(routine.PickupTime);
            //                DateTime mainRoutePickupDateTime = routineDate
            //                    .ToDateTime(TimeOnly.FromTimeSpan(mainRouteRoutine.PickupTime))/*.AddMinutes(30)*/;

            //                //if (pickupDateTime < mainRoutePickupDateTime)
            //                //{
            //                //    // Newly setup pickup time is earlier than the main route pickup + 30 minutes
            //                //    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                //        $"Tuyến đường chiều đi có lịch trình cho ngày {routineDate} vào lúc " +
            //                //        $"{TimeOnly.FromTimeSpan(mainRouteRoutine.PickupTime)} nhưng lịch trình cho tuyến đường chiều về cho ngày này " +
            //                //        $"lại được xếp sớm hơn quá 30 phút ({routine.PickupTime})!!");
            //                //}

            //                if ((pickupDateTime - mainRoutePickupDateTime).TotalMinutes <= travelTime + 10)
            //                {
            //                    throw new ApplicationException($"Thiết lập lịch trình cho tuyến khứ hồi không thành công! " +
            //                        $"Thời gian di chuyến giữa chuyến đi và về ước tính {Math.Round(travelTime, 0) + 10} phút, vui lòng " +
            //                        $"thiết lập lịch trình với thời gian đón phù hợp!");
            //                }
            //            }
            //        }
            //    }
            //}

            IEnumerable<Route> routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => r.UserId.Equals(route.UserId)
                    && r.Status == RouteStatus.ACTIVE), cancellationToken: cancellationToken);
            IEnumerable<Guid> routeIds = routes.Select(r => r.Id);

            if (isUpdate)
            {
                routeIds = routeIds.Where(r => !r.Equals(route.Id));
                if (route.Type == RouteType.ROUND_TRIP && checkForRoundTripRoutines)
                {
                    if (route.RoundTripRouteId.HasValue)
                    {
                        // The main route

                        // Get The roundtrip route
                        Route roundTripRoute = await work.Routes.GetAsync(route.RoundTripRouteId.Value,
                            cancellationToken: cancellationToken);
                        routeIds = routeIds.Where(r => !r.Equals(roundTripRoute.Id));
                    }
                    else
                    {
                        // The RoundTrip route

                        // Get the main route
                        Route mainRoute = await work.Routes.GetAsync(r => r.RoundTripRouteId.HasValue &&
                            r.RoundTripRouteId.Value.Equals(route.Id),
                            cancellationToken: cancellationToken);
                        routeIds = routeIds.Where(r => !r.Equals(mainRoute.Id));
                    }
                }
            }

            if (!routeIds.Any())
            {
                return;
            }

            IEnumerable<RouteRoutine> currentRouteRoutines =
                await work.RouteRoutines.GetAllAsync(query => query.Where(
                        r => routeIds.Contains(r.RouteId)
                        && r.Status == RouteRoutineStatus.ACTIVE), cancellationToken: cancellationToken);

            if (currentRouteRoutines.Any())
            {
                Station startStation = await work.Stations
                   .GetAsync(route.StartStationId, cancellationToken: cancellationToken);
                Station endStation = await work.Stations
                    .GetAsync(route.EndStationId, cancellationToken: cancellationToken);

                double travelTime = await GoogleMapsApiUtilities.GetDurationBetweenTwoPointsAsync(
                    new Models.GoogleMaps.GoogleMapPoint
                    {
                        Latitude = startStation.Latitude,
                        Longitude = startStation.Longitude
                    },
                    new Models.GoogleMaps.GoogleMapPoint
                    {
                        Latitude = endStation.Latitude,
                        Longitude = endStation.Longitude
                    },
                    cancellationToken);

                Dictionary<Guid, double> routeDurations = new Dictionary<Guid, double>();
                //foreach (Route routeCalDuration in routes)
                //{

                //}
                currentRouteRoutines = currentRouteRoutines.OrderBy(r => r.RoutineDate)
                    .ThenBy(r => r.PickupTime);

                IEnumerable<DateTimeRange> currentRanges =
                from routine in currentRouteRoutines
                select new DateTimeRange(
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.RoutineDate), TimeOnly.FromTimeSpan(routine.PickupTime)),
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.RoutineDate), TimeOnly.FromTimeSpan(routine.PickupTime)).AddMinutes(travelTime + 10)
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

                        if (currentRanges.Any(r => r.StartDateTime == added.StartDateTime && r.EndDateTime == added.EndDateTime))
                        {
                            // finalRanges[i + 1] is current
                            current = finalRanges[i + 1];
                            added = finalRanges[i];
                        }
                        current.IsOverlap(added,
                            $"Hai khoảng thời gian lịch trình đã bị trùng lặp với lịch trình " +
                            $"đã được lưu trước đó " +
                            $"(Lịch trình đã được lưu trước đó: {current.StartDateTime} và " +
                            $"lịch trình mới: {added.StartDateTime})");
                    }
                }
                //}
                //else 
                ////if (route.RouteType == RouteType.EVERY_ROUTE_EVERY_TIME)
                //{
                //    // Every time
                //    if (routines.Count == 0)
                //    {
                //        throw new ApplicationException("Không có lịch trình nào được thiết lập!!");
                //    }
                //    foreach (RouteRoutineListItemModel routine in routines)
                //    {
                //        if (routine.RoutineDate == null)
                //        {
                //            throw new ApplicationException("Lịch trình cần được thiết lập ngày đi!!");
                //        }
                //        if (routine.StartTime.HasValue || routine.EndTime.HasValue)
                //        {
                //            throw new ApplicationException("Lịch trình không cần thiết lập khung thời gian!!");
                //        }
                //    }
                //}
                //{
                //    // SPECIFIC_ROUTE_EVERY_TIME
                //    foreach (RouteRoutineListItemModel routine in routines)
                //    {
                //        if (routine.RoutineDate.HasValue
                //            || routine.StartTime.HasValue
                //            || routine.EndTime.HasValue)
                //        {
                //            throw new ApplicationException("Tuyến đường được thiết lập không cần khung thời gian!!");
                //        }
                //    }
                //}
            }
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
