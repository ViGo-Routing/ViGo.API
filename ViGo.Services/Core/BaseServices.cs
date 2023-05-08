using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Core;
using ViGo.Repository.Core;

namespace ViGo.Services.Core
{
    public abstract class BaseServices<T> where T : BaseEntity
    {
        protected IUnitOfWork work;

        public BaseServices(IUnitOfWork work)
        {
            this.work = work;
        }
    }
}
