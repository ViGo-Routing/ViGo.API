using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.RouteRoutines;
using ViGo.Models.RouteStations;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities.Validator;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;

namespace ViGo.Services
{
    public class RouteRoutineServices : BaseServices<RouteRoutine>
    {
        public RouteRoutineServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<IEnumerable<RouteRoutineViewModel>> 
            GetRouteRoutinesAsync(Guid routeId)
        {
            IEnumerable<RouteRoutine> routeRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(routeId)));

            IEnumerable<RouteRoutineViewModel> models =
                from routeRoutine in routeRoutines
                select new RouteRoutineViewModel(routeRoutine);
            models = models.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.StartTime);

            return models;
        }

        public async Task<IEnumerable<RouteRoutine>> CreateRouteRoutinesAsync(RouteRoutineCreateEditModel model)
        {
            Route? route = await work.Routes.GetAsync(model.RouteId);
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
                    r => r.RouteId.Equals(model.RouteId)));
            if (currentRoutines.Any())
            {
                throw new ApplicationException("Tuyến đường này đã được thiết lập lịch trình từ trước! " +
                    "Vui lòng dùng tính năng cập nhật lịch trình!");
            }

            foreach (RouteRoutineListItemModel routine in model.RouteRoutines)
            {
                IsValidRoutine(routine);
            }
            await IsValidRoutines(model.RouteRoutines, route);
            IList<RouteRoutine> routeRoutines =
                (from routine in model.RouteRoutines
                select new RouteRoutine
                {
                    RouteId = model.RouteId,
                    RoutineDate = routine.RoutineDate.ToDateTime(TimeOnly.MinValue),
                    StartTime = routine.StartTime.ToTimeSpan(),
                    EndTime = routine.EndTime.ToTimeSpan(),
                    Status = RouteRoutineStatus.ACTIVE
                }).ToList();
            await work.RouteRoutines.InsertAsync(routeRoutines);
            await work.SaveChangesAsync();
            routeRoutines = routeRoutines.OrderBy(r => r.RoutineDate)
                .ThenBy(r => r.StartTime).ToList();

            return routeRoutines;
        }

        public async Task<IEnumerable<RouteRoutine>> UpdateRouteRoutinesAsync(RouteRoutineCreateEditModel model)
        {
            Route? route = await work.Routes.GetAsync(model.RouteId);
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

            foreach (RouteRoutineListItemModel routine in model.RouteRoutines)
            {
                IsValidRoutine(routine);
            }

            // Check for existing Routines
            IEnumerable<RouteRoutine> currentRoutines = await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(model.RouteId)));

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
                } else
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
                await IsValidRoutines(routinesModel.ToList(), route, true);
                routeRoutines =
                    (from routine in routinesModel
                     select new RouteRoutine
                    {
                        RouteId = model.RouteId,
                        RoutineDate = routine.RoutineDate.ToDateTime(TimeOnly.MinValue),
                        StartTime = routine.StartTime.ToTimeSpan(),
                        EndTime = routine.EndTime.ToTimeSpan(),
                        Status = RouteRoutineStatus.ACTIVE
                    }).ToList();
                await work.RouteRoutines.InsertAsync(routeRoutines);
            }
            
            await work.SaveChangesAsync();

            IEnumerable<RouteRoutine> updatedRouteRoutines = (await work.RouteRoutines
                .GetAllAsync(query => query.Where(
                    r => r.RouteId.Equals(route.Id))))
                    .OrderBy(r => r.RoutineDate)
                    .ThenBy(r => r.StartTime);

            return updatedRouteRoutines;
        }

        #region Validation

        private void IsValidRoutine(RouteRoutineListItemModel routine)
        {
            DateTime startDateTime = DateTimeUtilities
                .ToDateTime(routine.RoutineDate, routine.StartTime);
            DateTime endDateTime = DateTimeUtilities.
                ToDateTime(routine.RoutineDate, routine.EndTime);

            DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();
            startDateTime.DateTimeValidate(
                minimum: vnNow,
                minErrorMessage: $"Thời gian bắt đầu lịch trình ở quá khứ (ngày: " +
                $"{routine.RoutineDate.ToShortDateString()}, " +
                $"giờ: {routine.StartTime.ToShortTimeString()})",
                maximum: endDateTime,
                maxErrorMessage: $"Thời gian kết thúc lịch trình không hợp lệ (ngày: " +
                $"{routine.RoutineDate.ToShortDateString()}, " +
                $"giờ: {routine.EndTime.ToShortTimeString()})"
                );
        }

        private async Task IsValidRoutines(IList<RouteRoutineListItemModel> routines,
            Route route, bool isUpdate = false)
        {
            IList<DateTimeRange> routineRanges =
                (from routine in routines
                 select new DateTimeRange(
                     DateTimeUtilities
                 .ToDateTime(routine.RoutineDate, routine.StartTime),
                     DateTimeUtilities.
                 ToDateTime(routine.RoutineDate, routine.EndTime)
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

            if (route == null)
            {
                throw new Exception("Thiếu thông tin tuyến đường! Vui lòng kiểm tra lại");
            }

            IEnumerable<Route> routes = await work.Routes
                .GetAllAsync(query => query.Where(
                    r => r.UserId.Equals(route.UserId)
                    && r.Status == RouteStatus.ACTIVE));
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
                await work.RouteRoutines.GetAllAsync(
                    query => query.Where(
                        r => routeIds.Contains(r.RouteId)
                        && r.Status == RouteRoutineStatus.ACTIVE)
            );

            if (currentRouteRoutines.Any())
            {
                IEnumerable<DateTimeRange> currentRanges =
                from routine in currentRouteRoutines
                select new DateTimeRange(
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.RoutineDate), TimeOnly.FromTimeSpan(routine.StartTime)),
                    DateTimeUtilities
                 .ToDateTime(DateOnly.FromDateTime(routine.RoutineDate), TimeOnly.FromTimeSpan(routine.EndTime))
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
            
        }
        #endregion
    }
}
