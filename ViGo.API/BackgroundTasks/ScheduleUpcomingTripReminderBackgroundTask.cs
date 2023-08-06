using Quartz;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;

namespace ViGo.API.BackgroundTasks
{
    public class ScheduleUpcomingTripReminderBackgroundTask : IHostedService
    {
        private ILogger<ScheduleUpcomingTripReminderBackgroundTask> _logger;
        private ISchedulerFactory _schedulerFactory;
        private IServiceScopeFactory _serviceScopeFactory;

        public ScheduleUpcomingTripReminderBackgroundTask(
            ILogger<ScheduleUpcomingTripReminderBackgroundTask> logger,
            ISchedulerFactory schedulerFactory,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _schedulerFactory = schedulerFactory;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Task is running - Schedule Upcoming Trip Reminder");
            await using (var scope = _serviceScopeFactory.CreateAsyncScope())
            {
                IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);

                // Schedule trip reminder
                IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
                await backgroundServices.ScheduleTripsReminderOnStartupAsync(scheduler, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Background Task is stopping - Schedule Upcoming Trip Reminder");

            return Task.CompletedTask;
        }
    }
}
