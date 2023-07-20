using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Notifications;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities.Extensions;

namespace ViGo.Services
{
    public partial class BackgroundServices : UseNotificationServices
    {
        public BackgroundServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        
    }
}
