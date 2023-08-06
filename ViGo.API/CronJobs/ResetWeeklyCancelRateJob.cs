using Quartz;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Runtime.CompilerServices;
using ViGo.Domain;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services;
using ViGo.Utilities.CronJobs;
using ViGo.Utilities;
using ViGo.Utilities.Extensions;

namespace ViGo.API.CronJobs
{
    public class ResetWeeklyCancelRateJob : IJob
    {
        private IServiceScopeFactory _serviceScopeFactory;
        private ILogger<ResetWeeklyCancelRateJob> _logger;

        public ResetWeeklyCancelRateJob(IServiceScopeFactory serviceScopeFactory, ILogger<ResetWeeklyCancelRateJob> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("========= BEGIN CRON JOBS =========");
            _logger.LogInformation("========= Reset Weekly Cancel Rate =========");

            try
            {
                await using (var scope = _serviceScopeFactory.CreateAsyncScope())
                {
                    IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                    CronJobServices cronJobServices = new CronJobServices(unitOfWork, _logger);

                    await cronJobServices.ResetUserWeeklyCancelRateAsync(context.CancellationToken);
                }

            } catch (Exception exception)
            {
                _logger.LogError(exception, "Exception thrown when executing CronJob.\n" +
                    "Details: {0}", exception.GeneratorErrorMessage());
            } finally
            {
                _logger.LogInformation("========= FINISH CRON JOBS =========");

            }

        }
    }

    public static class ResetWeeklyCancelRateJobConfiguration
    {
        public static IServiceCollectionQuartzConfigurator ConfigureResetWeeklyCancelRateJob(
            this IServiceCollectionQuartzConfigurator quartzConfigurator)
        {
            JobKey resetWeeklyCancelRateJobKey = new JobKey(CronJobIdentities.RESET_WEEKLY_CANCEL_RATE_JOBKEY);
            quartzConfigurator.AddJob<ResetWeeklyCancelRateJob>(options =>
                options.WithIdentity(resetWeeklyCancelRateJobKey)
                    .StoreDurably()
                    .WithDescription("Reset every user's Weekly Cancel Rate to 0")
            );

            string resetWeeklyCancelRateSchedule = "0 0 0 ? * MON *";

            quartzConfigurator.AddTrigger(options => options
                .ForJob(resetWeeklyCancelRateJobKey)
                .WithIdentity(CronJobIdentities.RESET_WEEKLY_CANCEL_RATE_TRIGGER_ID)
                .WithDescription("Reset every user's Weekly Cancel Rate to 0")
                .WithCronSchedule(resetWeeklyCancelRateSchedule,
                    scheduleBuilder =>
                    {
                        scheduleBuilder.InTimeZone(DateTimeUtilities.GetVnTimeZoneInfo);
                    }));

            return quartzConfigurator;
        }
    }
}
