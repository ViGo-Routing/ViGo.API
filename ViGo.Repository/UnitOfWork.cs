using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Repository.Core;

namespace ViGo.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
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
        public IRepository<RouteStation> RouteStations { get; }
        public IRepository<Setting> Settings { get; }
        public IRepository<Station> Stations { get; }
        public IRepository<User> Users { get; }
        public IRepository<UserLicense> UserLicenses { get; }
        public IRepository<Vehicle> Vehicles { get; }
        public IRepository<VehicleType> VehicleTypes { get; }
        public IRepository<Wallet> Wallets { get; }
        public IRepository<WalletTransaction> WalletTransactions { get; }
        #endregion


        #region Constructor
        public UnitOfWork(
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
            IRepository<RouteStation> routeStations, 
            IRepository<Setting> settings, 
            IRepository<Station> stations, 
            IRepository<User> users, 
            IRepository<UserLicense> userLicenses, 
            IRepository<Vehicle> vehicles, 
            IRepository<VehicleType> vehicleTypes, 
            IRepository<Wallet> wallets, 
            IRepository<WalletTransaction> walletTransactions)
        {
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
            RouteStations = routeStations;
            Settings = settings;
            Stations = stations;
            Users = users;
            UserLicenses = userLicenses;
            Vehicles = vehicles;
            VehicleTypes = vehicleTypes;
            Wallets = wallets;
            WalletTransactions = walletTransactions;
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

        public Task<int> SaveChangesAsync()
        {
            throw new NotImplementedException();
        }


    }
}
