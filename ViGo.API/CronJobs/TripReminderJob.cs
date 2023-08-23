using Quartz;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.CronJobs;
using ViGo.Utilities.Extensions;

namespace ViGo.API.CronJobs
{
    public class TripReminderJob : IJob
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private ILogger<ResetWeeklyCancelRateJob> _logger;

        public TripReminderJob(IServiceScopeFactory serviceScopeFactory, ILogger<ResetWeeklyCancelRateJob> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("========= BEGIN CRON JOBS =========");
            _logger.LogInformation("========= Trip Reminder =========");

            try
            {
                JobDataMap jobData = context.MergedJobDataMap;
                Guid bookingDetailId = jobData.GetGuid(CronJobIdentities.BOOKING_DETAIL_ID_JOB_DATA);

                await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                {
                    IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                    CronJobServices cronJobServices = new CronJobServices(unitOfWork, _logger);

                    await cronJobServices.RemindForTripAsync(bookingDetailId, context.CancellationToken);
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

    public static class TripReminderJobConfiguration
    {
        public static IServiceCollectionQuartzConfigurator ConfigureTripReminderJob(
            this IServiceCollectionQuartzConfigurator quartzConfigurator)
        {
            JobKey tripReminderJobKey = new JobKey(CronJobIdentities.UPCOMING_TRIP_NOTIFICATION_JOBKEY);
            quartzConfigurator.AddJob<TripReminderJob>(options =>
                options.WithIdentity(tripReminderJobKey)
                    .StoreDurably()
                    .WithDescription("Send notification to user about upcoming trip")
            );

            //string tripReminderSchedule = "0 0 0 ? * MON *";

            //quartzConfigurator.AddTrigger(options => options
            //    .ForJob(tripReminderJobKey)
            //    .WithIdentity(CronJobIdentities.UPCOMING_TRIP_NOTIFICATION_TRIGGER_ID)
            //    .WithCronSchedule(tripReminderSchedule,
            //        scheduleBuilder =>
            //        {
            //            scheduleBuilder.InTimeZone(DateTimeUtilities.GetVnTimeZoneInfo);
            //        }));

            return quartzConfigurator;
        }
    }
}
