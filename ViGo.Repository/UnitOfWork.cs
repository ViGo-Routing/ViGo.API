using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using ViGo.Domain;
using ViGo.Repository.Core;

namespace ViGo.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ViGoDBContext context;

        #region Repositories
        public IRepository<Booking> Bookings { get; }
        public IRepository<BookingDetail> BookingDetails { get; }
        public IRepository<Event> Events { get; }
        public IRepository<Fare> Fares { get; }
        public IRepository<FarePolicy> FarePolicies { get; }
        public IRepository<Notification> Notifications { get; }
        public IRepository<Promotion> Promotions { get; }
        public IRepository<Report> Reports { get; }
        public IRepository<Route> Routes { get; }
        public IRepository<RouteRoutine> RouteRoutines { get; }
        public IRepository<Setting> Settings { get; }
        public IRepository<Station> Stations { get; }
        public IRepository<User> Users { get; }
        public IRepository<UserLicense> UserLicenses { get; }
        public IRepository<Vehicle> Vehicles { get; }
        public IRepository<VehicleType> VehicleTypes { get; }
        public IRepository<Wallet> Wallets { get; }
        public IRepository<WalletTransaction> WalletTransactions { get; }
        #endregion

        private IDistributedCache cache { get; }

        #region Constructor
        public UnitOfWork(
            ViGoDBContext context,
            IRepository<Booking> bookings,
            IRepository<BookingDetail> bookingDetails,
            IRepository<Event> events,
            IRepository<Fare> fares,
            IRepository<FarePolicy> farePolicies,
            IRepository<Notification> notifications,
            IRepository<Promotion> promotions,
            IRepository<Report> reports,
            IRepository<Route> routes,
            IRepository<RouteRoutine> routeRoutines,
            IRepository<Setting> settings,
            IRepository<Station> stations,
            IRepository<User> users,
            IRepository<UserLicense> userLicenses,
            IRepository<Vehicle> vehicles,
            IRepository<VehicleType> vehicleTypes,
            IRepository<Wallet> wallets,
            IRepository<WalletTransaction> walletTransactions,
            IDistributedCache cache)
        {
            this.context = context;
            Bookings = bookings;
            BookingDetails = bookingDetails;
            Events = events;
            Fares = fares;
            FarePolicies = farePolicies;
            Notifications = notifications;
            Promotions = promotions;
            Reports = reports;
            Routes = routes;
            RouteRoutines = routeRoutines;
            Settings = settings;
            Stations = stations;
            Users = users;
            UserLicenses = userLicenses;
            Vehicles = vehicles;
            VehicleTypes = vehicleTypes;
            Wallets = wallets;
            WalletTransactions = walletTransactions;
            this.cache = cache;
        }

        public UnitOfWork(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetRequiredService<ViGoDBContext>();
            Bookings = serviceProvider.GetRequiredService<IRepository<Booking>>();
            BookingDetails = serviceProvider.GetRequiredService<IRepository<BookingDetail>>(); ;
            Events = serviceProvider.GetRequiredService<IRepository<Event>>();
            Fares = serviceProvider.GetRequiredService<IRepository<Fare>>();
            FarePolicies = serviceProvider.GetRequiredService<IRepository<FarePolicy>>();
            Notifications = serviceProvider.GetRequiredService<IRepository<Notification>>();
            Promotions = serviceProvider.GetRequiredService<IRepository<Promotion>>();
            Reports = serviceProvider.GetRequiredService<IRepository<Report>>();
            Routes = serviceProvider.GetRequiredService<IRepository<Route>>();
            RouteRoutines = serviceProvider.GetRequiredService<IRepository<RouteRoutine>>();
            Settings = serviceProvider.GetRequiredService<IRepository<Setting>>();
            Stations = serviceProvider.GetRequiredService<IRepository<Station>>();
            Users = serviceProvider.GetRequiredService<IRepository<User>>();
            UserLicenses = serviceProvider.GetRequiredService<IRepository<UserLicense>>();
            Vehicles = serviceProvider.GetRequiredService<IRepository<Vehicle>>();
            VehicleTypes = serviceProvider.GetRequiredService<IRepository<VehicleType>>();
            Wallets = serviceProvider.GetRequiredService<IRepository<Wallet>>();
            WalletTransactions = serviceProvider.GetRequiredService<IRepository<WalletTransaction>>();
            cache = serviceProvider.GetRequiredService<IDistributedCache>();
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //context.Dispose();
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            int result = await context.SaveChangesAsync(cancellationToken);

            // Save changes to redis
            await Bookings.SaveChangesToRedisAsync(cancellationToken);
            await BookingDetails.SaveChangesToRedisAsync(cancellationToken);
            await Events.SaveChangesToRedisAsync(cancellationToken);
            await Fares.SaveChangesToRedisAsync(cancellationToken);
            await FarePolicies.SaveChangesToRedisAsync(cancellationToken);
            await Notifications.SaveChangesToRedisAsync(cancellationToken);
            await Promotions.SaveChangesToRedisAsync(cancellationToken);
            await Reports.SaveChangesToRedisAsync(cancellationToken);
            await Routes.SaveChangesToRedisAsync(cancellationToken);
            await RouteRoutines.SaveChangesToRedisAsync(cancellationToken);
            await Settings.SaveChangesToRedisAsync(cancellationToken);
            await Stations.SaveChangesToRedisAsync(cancellationToken);
            await Users.SaveChangesToRedisAsync(cancellationToken);
            await UserLicenses.SaveChangesToRedisAsync(cancellationToken);
            await Vehicles.SaveChangesToRedisAsync(cancellationToken);
            await VehicleTypes.SaveChangesToRedisAsync(cancellationToken);
            await Wallets.SaveChangesToRedisAsync(cancellationToken);
            await WalletTransactions.SaveChangesToRedisAsync(cancellationToken);

            return result;
        }

        public async Task FlushAllRedisAsync(CancellationToken cancellationToken = default)
        {
            await Bookings.RemoveFromRedisAsync(cancellationToken);
            await BookingDetails.RemoveFromRedisAsync(cancellationToken);
            await Events.RemoveFromRedisAsync(cancellationToken);
            await Fares.RemoveFromRedisAsync(cancellationToken);
            await FarePolicies.RemoveFromRedisAsync(cancellationToken);
            await Notifications.RemoveFromRedisAsync(cancellationToken);
            await Promotions.RemoveFromRedisAsync(cancellationToken);
            await Reports.RemoveFromRedisAsync(cancellationToken);
            await Routes.RemoveFromRedisAsync(cancellationToken);
            await RouteRoutines.RemoveFromRedisAsync(cancellationToken);
            await Settings.RemoveFromRedisAsync(cancellationToken);
            await Stations.RemoveFromRedisAsync(cancellationToken);
            await Users.RemoveFromRedisAsync(cancellationToken);
            await UserLicenses.RemoveFromRedisAsync(cancellationToken);
            await Vehicles.RemoveFromRedisAsync(cancellationToken);
            await VehicleTypes.RemoveFromRedisAsync(cancellationToken);
            await Wallets.RemoveFromRedisAsync(cancellationToken);
            await WalletTransactions.RemoveFromRedisAsync(cancellationToken);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await context.Database.BeginTransactionAsync(cancellationToken);
        }
    }
}
