using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using ViGo.API.BackgroundTasks;
using ViGo.API.SignalR;
using ViGo.API.SignalR.Core;
using ViGo.Domain;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Utilities.Configuration;

namespace ViGo.API
{
    public static class ViGoDependencyInjection
    {
        public static IServiceCollection AddViGoDependencyInjection(
            this IServiceCollection services,
            IWebHostEnvironment env)
        {
            #region SignalR
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });

            services.AddTransient<ISignalRService, SignalRService>();
            #endregion

            #region Firebase
            var credential = GoogleCredential.FromFile(ViGoConfiguration.FirebaseCredentialFile);
            FirebaseApp.Create(new AppOptions
            {
                Credential = credential
            });

            #endregion

            #region DbContext
            services.AddDbContext<ViGoDBContext>(options =>
            {
                if (env.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.UseSqlServer(ViGoConfiguration.ConnectionString("ViGoDb_PhongNT"));
                    //options.UseSqlServer(ViGoConfiguration.ConnectionString("ViGoDb_Azure"));
                }
                else
                {
                    options.UseSqlServer(ViGoConfiguration.ConnectionString("ViGoDb_Azure"));
                }
            });
            #endregion

            #region Repositories
            services.AddScoped<IRepository<Booking>, EntityRepository<Booking>>();
            services.AddScoped<IRepository<BookingDetail>, EntityRepository<BookingDetail>>();
            services.AddScoped<IRepository<Event>, EntityRepository<Event>>();
            services.AddScoped<IRepository<Fare>, EntityRepository<Fare>>();
            services.AddScoped<IRepository<FarePolicy>, EntityRepository<FarePolicy>>();
            services.AddScoped<IRepository<Notification>, EntityRepository<Notification>>();
            services.AddScoped<IRepository<Promotion>, EntityRepository<Promotion>>();
            services.AddScoped<IRepository<Report>, EntityRepository<Report>>();
            services.AddScoped<IRepository<ViGo.Domain.Route>, EntityRepository<ViGo.Domain.Route>>();
            services.AddScoped<IRepository<RouteRoutine>, EntityRepository<RouteRoutine>>();
            services.AddScoped<IRepository<RouteStation>, EntityRepository<RouteStation>>();
            services.AddScoped<IRepository<Setting>, EntityRepository<Setting>>();
            services.AddScoped<IRepository<Station>, EntityRepository<Station>>();
            services.AddScoped<IRepository<User>, EntityRepository<User>>();
            services.AddScoped<IRepository<UserLicense>, EntityRepository<UserLicense>>();
            services.AddScoped<IRepository<Vehicle>, EntityRepository<Vehicle>>();
            services.AddScoped<IRepository<VehicleType>, EntityRepository<VehicleType>>();
            services.AddScoped<IRepository<Wallet>, EntityRepository<Wallet>>();
            services.AddScoped<IRepository<WalletTransaction>, EntityRepository<WalletTransaction>>();
            #endregion

            #region UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            #endregion

            #region Background Task
            services.AddHostedService<QueuedHostedServices>();
            services.AddSingleton<IBackgroundTaskQueue>(context =>
            {
                try
                {
                    return new BackgroundTaskQueue(ViGoConfiguration.QueueCapacity);
                } catch (Exception)
                {
                    return new BackgroundTaskQueue(100);
                }
            });
            #endregion

            return services;
        }
    }
}
