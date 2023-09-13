using Quartz;
using ViGo.Utilities.Configuration;
using ViGo.Utilities.CronJobs;

namespace ViGo.API.CronJobs
{
    public static class ViGoCronJobConfiguration
    {
        public static IServiceCollection RegisterCronJobs(
            this IServiceCollection services, IWebHostEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    services.Configure<QuartzOptions>(ViGoConfiguration.QuartzConfiguration
            //        .GetSection("Local"));
            //}
            //else
            //{
            //    services.Configure<QuartzOptions>(ViGoConfiguration.QuartzConfiguration
            //            .GetSection("Production"));
            //}
            services.Configure<QuartzOptions>(ViGoConfiguration.QuartzConfiguration);

            services.AddQuartz(q =>
            {
                q.SchedulerId = CronJobIdentities.SCHEDULER_ID;

                q.UseMicrosoftDependencyInjectionJobFactory();

                q.ConfigureResetWeeklyCancelRateJob();

                if (!env.IsDevelopment())
                {
                    q.ConfigureTripReminderJob();

                }

                //q.ConfigureCheckTransactionStatusJob();

            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}
