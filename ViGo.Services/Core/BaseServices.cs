using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Core;
using ViGo.Repository.Core;

namespace ViGo.Services.Core
{
    public abstract class BaseServices
    {
        protected IUnitOfWork work;
        protected ILogger _logger;

        public BaseServices(IUnitOfWork work, ILogger logger)
        {
            this.work = work;
            _logger = logger;
        }
    }
}
