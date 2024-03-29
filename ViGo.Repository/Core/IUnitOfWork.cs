﻿using Microsoft.EntityFrameworkCore.Storage;
using ViGo.Domain;

namespace ViGo.Repository.Core
{
    public interface IUnitOfWork : IDisposable
    {
        #region Repositories
        IRepository<Booking> Bookings { get; }
        IRepository<BookingDetail> BookingDetails { get; }
        //IRepository<Event> Events { get; }
        IRepository<Fare> Fares { get; }
        IRepository<FarePolicy> FarePolicies { get; }
        IRepository<Notification> Notifications { get; }
        //IRepository<Promotion> Promotions { get; }
        IRepository<Report> Reports { get; }
        IRepository<Route> Routes { get; }
        IRepository<RouteRoutine> RouteRoutines { get; }
        IRepository<Setting> Settings { get; }
        IRepository<Station> Stations { get; }
        IRepository<User> Users { get; }
        IRepository<UserLicense> UserLicenses { get; }
        IRepository<Vehicle> Vehicles { get; }
        IRepository<VehicleType> VehicleTypes { get; }
        IRepository<Wallet> Wallets { get; }
        IRepository<WalletTransaction> WalletTransactions { get; }
        #endregion

        /// <summary>
        /// Save changes to database
        /// </summary>
        /// <returns></returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        Task FlushAllRedisAsync(CancellationToken cancellationToken = default);
    }
}
