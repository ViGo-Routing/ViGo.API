﻿using Microsoft.Extensions.Logging;
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
    public class EventServices : BaseServices
    {
        public EventServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }
    }
}
