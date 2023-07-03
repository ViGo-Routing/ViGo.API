using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.RouteRoutines;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities.Validator;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using Microsoft.Extensions.Logging;
using ViGo.Repository.Pagination;
using Microsoft.AspNetCore.Http;

namespace ViGo.Services
{
    public class RouteRoutineServices : BaseServices
    {
        public RouteRoutineServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<RouteRoutineViewModel>>
            GetRouteRoutinesAsync(Guid routeId,
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(routeId)), cancellationToken: cancellationToken);

            int totalRecords = routeRoutines.Count();

            routeRoutines = routeRoutines.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<RouteRoutineViewModel> models =
                from routeRoutine in routeRoutines
                select new RouteRoutineViewModel(routeRoutine);
            models = models.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.StartTime);

            return models.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize, totalRecords, context);
        }

        public async Task<IEnumerable<RouteRoutine>> CreateRouteRoutinesAsync(RouteRoutineCreateUpdateModel model,
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
            if (currentRoutines.Any())
            {
                throw new ApplicationException("Tuyến đường này đã được thiết lập lịch trình từ trước! " +
                    "Vui lòng dùng tính năng cập nhật lịch trình!");
            }

            foreach (RouteRoutineListItemModel routine in model.RouteRoutines)
            {
                IsValidRoutine(routine);
            }

            await IsValidRoutines(model.RouteRoutines, route, cancellationToken: cancellationToken);
            IList<RouteRoutine> routeRoutines =
                (from routine in model.RouteRoutines
                 select new RouteRoutine
                 {
                     RouteId = model.RouteId,
                     RoutineDate = routine.RoutineDate.ToDateTime(TimeOnly.MinValue),
                     StartTime = routine.StartTime.ToTimeSpan(),
                     Status = RouteRoutineStatus.ACTIVE
                 }).ToList();
            await work.RouteRoutines.InsertAsync(routeRoutines, cancellationToken: cancellationToken);
            await work.SaveChangesAsync(cancellationToken);
            routeRoutines = routeRoutines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.StartTime).ToList();

            return routeRoutines;
        }

        public async Task<IEnumerable<RouteRoutine>> UpdateRouteRoutinesAsync(RouteRoutineCreateUpdateModel model,
            CancellationToken cancellationToken)
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
                if (!route.UserId.Equals(IdentityUtilities.GetCurrentUserId()))
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!");
                }
            }

            foreach (RouteRoutineListItemModel routine in model.RouteRoutines)
            {
                IsValidRoutine(routine);
            }

            // Check for existing Routines
            IEnumerable<RouteRoutine> currentRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(model.RouteId)), cancellationToken: cancellationToken);

            if (!currentRoutines.Any())
            {
                throw new ApplicationException("Tuyến đường này chưa được thiết lập lịch trình!! " +
                    "Vui lòng dùng chức năng Tạo lịch trình!");
            }
            currentRoutines = currentRoutines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.StartTime);
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

                    //routineToDelete.Add(oldRoutine.Id);
                    await work.RouteRoutines.DeleteAsync(oldRoutine, isSoftDelete: false);
                }
                else
                {
                    model.RouteRoutines.Remove(routineModel);
                }
            }

            //if (routineToDelete.Count > 0)
            //{

            //}

            IList<RouteRoutine> routeRoutines = new List<RouteRoutine>();
            IEnumerable<RouteRoutineListItemModel> routinesModel = model.RouteRoutines;
            if (routinesModel.Any())
            {
                await IsValidRoutines(routinesModel.ToList(), route, true, cancellationToken);
                routeRoutines =
                    (from routine in routinesModel
                     select new RouteRoutine
                     {
                         RouteId = model.RouteId,
                         RoutineDate = routine.RoutineDate.ToDateTime(TimeOnly.MinValue),
                         StartTime = routine.StartTime.ToTimeSpan(),
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
                    .ThenBy(r => r.StartTime);

            return updatedRouteRoutines;
        }

        #region Validation

        private void IsValidRoutine(RouteRoutineListItemModel routine)
        {
                DateTime startDateTime = DateTimeUtilities
                .ToDateTime(routine.RoutineDate, routine.StartTime);
                //DateTime endDateTime = DateTimeUtilities.
                //    ToDateTime(routine.RoutineDate, routine.StartTime.AddMinutes(30));

                DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();
                startDateTime.DateTimeValidate(
                    minimum: vnNow,
                    minErrorMessage: $"Thời gian bắt đầu lịch trình ở quá khứ (ngày: " +
                    $"{routine.RoutineDate.ToShortDateString()}, " +
                    $"giờ: {routine.StartTime.ToShortTimeString()})"
                    //maximum: endDateTime,
                    //maxErrorMessage: $"Thời gian kết thúc lịch trình không hợp lệ (ngày: " +
                    //$"{routine.RoutineDate.ToShortDateString()}, " +
                    //$"giờ: {routine.EndTime.ToShortTimeString()})"
                    );
        }

        private async Task IsValidRoutines(IList<RouteRoutineListItemModel> routines,
            Route route, bool isUpdate = false, CancellationToken cancellationToken = default)
        {
            if (route == null)
            {
                throw new Exception("Thiếu thông tin tuyến đường! Vui lòng kiểm tra lại");
            }

            //if (route.RouteType == RouteType.SPECIFIC_ROUTE_SPECIFIC_TIME ||
            //    route.RouteType == RouteType.EVERY_ROUTE_SPECIFIC_TIME)
            //{
            IList<DateTimeRange> routineRanges = new List<DateTimeRange>();

            foreach (RouteRoutineListItemModel routine in routines)
            {
                routineRanges.Add(new DateTimeRange(
                 DateTimeUtilities
                 .ToDateTime(routine.RoutineDate, routine.StartTime),
                     DateTimeUtilities.
                 ToDateTime(routine.RoutineDate, routine.StartTime.AddMinutes(30))
                 ));
            }

            for (int i = 0; i < routineRanges.Count - 1; i++)
            {
                DateTimeRange first = routineRanges[i];
                DateTimeRange second = routineRanges[i + 1];

                first.IsOverlap(second,
                    $"Hai khoảng thời gian lịch trình phải cách nhau ít nhất 30 phút" +
                    $"({first.StartDateTime} và " +
                    $"{second.StartDateTime})");
            }

            IEnumerable<Route> routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => r.UserId.Equals(route.UserId)
                    && r.Status == RouteStatus.ACTIVE), cancellationToken: cancellationToken);
            IEnumerable<Guid> routeIds = routes.Select(r => r.Id);

            if (isUpdate)
            {
                routeIds = routeIds.Where(r => !r.Equals(route.Id));
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
                Dictionary<Guid, double> routeDurations = new Dictionary<Guid, double>();
                foreach (Route routeCalDuration in routes)
                {
                    
                }
                IEnumerable<DateTimeRange> currentRanges =
                from routine in currentRouteRoutines
                select new DateTimeRange(
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.RoutineDate), TimeOnly.FromTimeSpan(routine.StartTime)),
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.RoutineDate), TimeOnly.FromTimeSpan(routine.StartTime.Add(TimeSpan.FromMinutes(30))))
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
                            $"đã được lưu trước đó (phải cách nhau ít nhất 30 phút) " +
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
        #endregion
    }
}
