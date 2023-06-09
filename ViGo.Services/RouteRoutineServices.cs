using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.RouteRoutines;
using ViGo.Repository.Core;
using ViGo.Services.Core;

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
            return models;
        }
    }
}
