using Quartz;
using ViGo.Repository.Core;
using ViGo.Repository;
using ViGo.Services;
using ViGo.Utilities.CronJobs;
using ViGo.Utilities.Extensions;

namespace ViGo.API.CronJobs
{
    public class CheckForPendingAssignTripJob : IJob
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private ILogger<ResetWeeklyCancelRateJob> _logger;

        public CheckForPendingAssignTripJob(IServiceScopeFactory serviceScopeFactory,
            ILogger<ResetWeeklyCancelRateJob> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("========= BEGIN CRON JOBS =========");
            _logger.LogInformation("========= Trip Check for Pending Assign =========");

            try
            {
                JobDataMap jobData = context.MergedJobDataMap;
                Guid bookingDetailId = jobData.GetGuid(CronJobIdentities.BOOKING_DETAIL_ID_JOB_DATA);

                await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                {
                    IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                    CronJobServices cronJobServices = new CronJobServices(unitOfWork, _logger);

                    await cronJobServices.CheckForDriverAssignedTripAsync(bookingDetailId, context.CancellationToken);

                }

            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Exception thrown when executing CronJob.\n" +
                    "Details: {0}", exception.GeneratorErrorMessage());
            }
            finally
            {
                _logger.LogInformation("========= FINISH CRON JOBS =========");

            }
        }
    }

    public static class CheckPendingAssignTripJobConfiguration
    {
        public static IServiceCollectionQuartzConfigurator ConfigurePendingTripJob(
            this IServiceCollectionQuartzConfigurator quartzConfigurator)
        {
            JobKey pendingTripJobKey = new JobKey(CronJobIdentities.UPCOMING_TRIP_PENDING_ASSIGN_NOTIFICATION_JOBKEY);
            quartzConfigurator.AddJob<TripReminderJob>(options =>
                options.WithIdentity(pendingTripJobKey)
                    .StoreDurably()
                    .WithDescription("Send notification to user about pending assign trip")
            );

            return quartzConfigurator;
        }
    }
}
