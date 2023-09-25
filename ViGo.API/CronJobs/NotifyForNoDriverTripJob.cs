using Quartz;
using ViGo.Repository.Core;
using ViGo.Repository;
using ViGo.Services;
using ViGo.Utilities.CronJobs;
using ViGo.Utilities.Extensions;

namespace ViGo.API.CronJobs
{
    public class NotifyForNoDriverTripJob : IJob
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private ILogger<ResetWeeklyCancelRateJob> _logger;

        public NotifyForNoDriverTripJob(IServiceScopeFactory serviceScopeFactory, 
            ILogger<ResetWeeklyCancelRateJob> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("========= BEGIN CRON JOBS =========");
            _logger.LogInformation("========= No Driver Trip =========");

            try
            {
                JobDataMap jobData = context.MergedJobDataMap;
                Guid bookingDetailId = jobData.GetGuid(CronJobIdentities.BOOKING_DETAIL_ID_JOB_DATA);

                await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                {
                    IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                    CronJobServices cronJobServices = new CronJobServices(unitOfWork, _logger);

                    await cronJobServices.NotifyForNoDriverTripAsync(bookingDetailId, context.CancellationToken);

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

    public static class NoDriverTripJobConfiguration
    {
        public static IServiceCollectionQuartzConfigurator ConfigureNoDriverTripJob(
            this IServiceCollectionQuartzConfigurator quartzConfigurator)
        {
            JobKey noDriverTripJobKey = new JobKey(CronJobIdentities.UPCOMING_TRIP_NO_DRIVER_NOTIFICATION_JOBKEY);
            quartzConfigurator.AddJob<TripReminderJob>(options =>
                options.WithIdentity(noDriverTripJobKey)
                    .StoreDurably()
                    .WithDescription("Send notification to user about no driver trip")
            );

            return quartzConfigurator;
        }
    }
}
