﻿using Microsoft.Extensions.Logging;
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

    public abstract class UseNotificationServices : BaseServices
    {
        protected NotificationServices notificationServices;

        protected UseNotificationServices(IUnitOfWork work, ILogger logger) 
            : base(work, logger)
        {
            notificationServices = new NotificationServices(work, logger);
        }
    }
}
