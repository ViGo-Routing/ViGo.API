using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class StationServices : BaseServices<Station>
    {
        public StationServices(IUnitOfWork work) : base(work)
        {
        }
    }
}
