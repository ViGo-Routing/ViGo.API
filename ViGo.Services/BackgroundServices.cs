using Microsoft.Extensions.Logging;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public partial class BackgroundServices : UseNotificationServices
    {
        public BackgroundServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }


    }
}
