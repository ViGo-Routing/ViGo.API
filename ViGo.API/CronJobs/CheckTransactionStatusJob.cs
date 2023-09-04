using Quartz;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.BackgroundTasks;
using ViGo.Utilities.CronJobs;
using ViGo.Utilities.Extensions;

namespace ViGo.API.CronJobs
{
    public class CheckTransactionStatusJob : IJob
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private IBackgroundTaskQueue _backgroundQueue;
        private ILogger<ResetWeeklyCancelRateJob> _logger;

        public CheckTransactionStatusJob(IServiceScopeFactory serviceScopeFactory,
            IBackgroundTaskQueue backgroundTaskQueue,
            ILogger<ResetWeeklyCancelRateJob> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _backgroundQueue = backgroundTaskQueue;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("========= BEGIN CRON JOBS =========");
            _logger.LogInformation("========= Check Transaction Status =========");

            try
            {
                JobDataMap jobData = context.MergedJobDataMap;
                Guid transactionId = jobData.GetGuid(CronJobIdentities.TRANSACTION_ID_JOB_DATA);
                string clientIpAddress = jobData.GetString(CronJobIdentities.CLIENT_IP_ADDRESS_JOB_DATA);

                await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                {
                    IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                    CronJobServices cronJobServices = new CronJobServices(unitOfWork, _logger);

                    await cronJobServices.CheckForTopupTransactionStatus(transactionId,
                        clientIpAddress, _backgroundQueue, _serviceScopeFactory, context.CancellationToken);
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

    public static class CheckTransactionStatusJobConfiguration
    {
        public static IServiceCollectionQuartzConfigurator ConfigureCheckTransactionStatusJob(
            this IServiceCollectionQuartzConfigurator quartzConfigurator)
        {
            JobKey checkTransactionStatusJobKey = new JobKey(CronJobIdentities.CHECK_TRANSACTION_STATUS_JOBKEY);
            quartzConfigurator.AddJob<CheckTransactionStatusJob>(options =>
                options.WithIdentity(checkTransactionStatusJobKey)
                    .StoreDurably()
                    .WithDescription("Check for transaction status")
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
