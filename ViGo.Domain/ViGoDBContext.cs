using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ViGo.Domain
{
    public partial class ViGoDBContext : DbContext
    {
        public ViGoDBContext()
        {
        }

        public ViGoDBContext(DbContextOptions<ViGoDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Booking> Bookings { get; set; } = null!;
        public virtual DbSet<BookingDetail> BookingDetails { get; set; } = null!;
        public virtual DbSet<Event> Events { get; set; } = null!;
        public virtual DbSet<Fare> Fares { get; set; } = null!;
        public virtual DbSet<FarePolicy> FarePolicies { get; set; } = null!;
        public virtual DbSet<Notification> Notifications { get; set; } = null!;
        public virtual DbSet<Promotion> Promotions { get; set; } = null!;
        public virtual DbSet<Report> Reports { get; set; } = null!;
        public virtual DbSet<Route> Routes { get; set; } = null!;
        public virtual DbSet<RouteRoutine> RouteRoutines { get; set; } = null!;
        public virtual DbSet<RouteStation> RouteStations { get; set; } = null!;
        public virtual DbSet<Setting> Settings { get; set; } = null!;
        public virtual DbSet<Station> Stations { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<UserLicense> UserLicenses { get; set; } = null!;
        public virtual DbSet<Vehicle> Vehicles { get; set; } = null!;
        public virtual DbSet<VehicleType> VehicleTypes { get; set; } = null!;
        public virtual DbSet<Wallet> Wallets { get; set; } = null!;
        public virtual DbSet<WalletTransaction> WalletTransactions { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("Booking");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.DaysOfWeek).HasMaxLength(100);

                entity.Property(e => e.EndDate).HasColumnType("date");

                entity.Property(e => e.StartDate).HasColumnType("date");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.BookingCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Customer)
                    .WithMany(p => p.BookingCustomers)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.EndRouteStation)
                    .WithMany(p => p.BookingEndRouteStations)
                    .HasForeignKey(d => d.EndRouteStationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Booking_RouteStation_EndStation");

                entity.HasOne(d => d.Promotion)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.PromotionId)
                    .HasConstraintName("FK_Booking_Promotion");

                entity.HasOne(d => d.StartRouteStation)
                    .WithMany(p => p.BookingStartRouteStations)
                    .HasForeignKey(d => d.StartRouteStationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Booking_RouteStation_StartStation");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.BookingUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.VehicleType)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.VehicleTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Booking_VehicleType");
            });

            modelBuilder.Entity<BookingDetail>(entity =>
            {
                entity.ToTable("BookingDetail");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Date).HasColumnType("date");

                entity.Property(e => e.Feedback).HasMaxLength(500);

                entity.HasOne(d => d.Booking)
                    .WithMany(p => p.BookingDetails)
                    .HasForeignKey(d => d.BookingId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BookingDetail_Booking");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.BookingDetailCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.CustomerRoute)
                    .WithMany(p => p.BookingDetailCustomerRoutes)
                    .HasForeignKey(d => d.CustomerRouteId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BookingDetail_Route_CustomerRoute");

                entity.HasOne(d => d.Driver)
                    .WithMany(p => p.BookingDetailDrivers)
                    .HasForeignKey(d => d.DriverId)
                    .HasConstraintName("FK_BookingDetail_User_Driver");

                entity.HasOne(d => d.DriverRoute)
                    .WithMany(p => p.BookingDetailDriverRoutes)
                    .HasForeignKey(d => d.DriverRouteId)
                    .HasConstraintName("FK_BookingDetail_Route_DriverRoute");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.BookingDetailUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Event");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Content).HasMaxLength(2000);

                entity.Property(e => e.Title).HasMaxLength(255);
            });

            modelBuilder.Entity<Fare>(entity =>
            {
                entity.ToTable("Fare");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.FareCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.FareUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.VehicleType)
                    .WithMany(p => p.Fares)
                    .HasForeignKey(d => d.VehicleTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Fare_VehicleType");
            });

            modelBuilder.Entity<FarePolicy>(entity =>
            {
                entity.ToTable("FarePolicy");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.FarePolicyCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Fare)
                    .WithMany(p => p.FarePolicies)
                    .HasForeignKey(d => d.FareId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FarePolicy_Fare");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.FarePolicyUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notification");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.Title).HasMaxLength(255);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.NotificationCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Event)
                    .WithMany(p => p.Notifications)
                    .HasForeignKey(d => d.EventId)
                    .HasConstraintName("FK_Notification_Event");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.NotificationUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.NotificationUsers)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotion");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Code).HasMaxLength(15);

                entity.Property(e => e.Description).HasMaxLength(2000);

                entity.Property(e => e.IsPercentage)
                    .IsRequired()
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.Name).HasMaxLength(255);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.PromotionCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.PromotionUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.VehicleType)
                    .WithMany(p => p.Promotions)
                    .HasForeignKey(d => d.VehicleTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Promotion_VehicleType");
            });

            modelBuilder.Entity<Report>(entity =>
            {
                entity.ToTable("Report");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Content).HasMaxLength(2000);

                entity.Property(e => e.Title).HasMaxLength(255);

                entity.HasOne(d => d.BookingDetail)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(d => d.BookingDetailId)
                    .HasConstraintName("FK_Report_BookingDetail");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.ReportCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Report_Report_CreatedBy");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.ReportUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.ReportUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Route>(entity =>
            {
                entity.ToTable("Route");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(255);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.RouteCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.EndStation)
                    .WithMany(p => p.RouteEndStations)
                    .HasForeignKey(d => d.EndStationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Route_Station_Station");

                entity.HasOne(d => d.StartStation)
                    .WithMany(p => p.RouteStartStations)
                    .HasForeignKey(d => d.StartStationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Route_Station_Start");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.RouteUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.RouteUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<RouteRoutine>(entity =>
            {
                entity.ToTable("RouteRoutine");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.EndDate).HasColumnType("date");

                entity.Property(e => e.StartDate).HasColumnType("date");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.RouteRoutineCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Route)
                    .WithMany(p => p.RouteRoutines)
                    .HasForeignKey(d => d.RouteId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RouteRoutine_Route");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.RouteRoutineUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.RouteRoutineUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<RouteStation>(entity =>
            {
                entity.ToTable("RouteStation");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.RouteStationCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.NextRouteStation)
                    .WithMany(p => p.InverseNextRouteStation)
                    .HasForeignKey(d => d.NextRouteStationId)
                    .HasConstraintName("FK_RouteStation_RouteStation_NextStation");

                entity.HasOne(d => d.Route)
                    .WithMany(p => p.RouteStations)
                    .HasForeignKey(d => d.RouteId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RouteStation_Route");

                entity.HasOne(d => d.Station)
                    .WithMany(p => p.RouteStations)
                    .HasForeignKey(d => d.StationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_RouteStation_Station");

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.RouteStationUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.ToTable("Setting");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Key).HasMaxLength(255);

                entity.Property(e => e.Value).HasMaxLength(2000);
            });

            modelBuilder.Entity<Station>(entity =>
            {
                entity.ToTable("Station");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Address).HasMaxLength(500);

                entity.Property(e => e.Name).HasMaxLength(255);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.StationCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.StationUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.DateOfBirth).HasColumnType("date");

                entity.Property(e => e.Email).HasMaxLength(255);

                entity.Property(e => e.Gender).HasDefaultValueSql("((0))");

                entity.Property(e => e.Name).HasMaxLength(255);

                entity.Property(e => e.Password).HasMaxLength(255);

                entity.Property(e => e.Phone).HasMaxLength(12);

                entity.Property(e => e.Photo).HasMaxLength(255);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.InverseCreatedByNavigation)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.InverseUpdatedByNavigation)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<UserLicense>(entity =>
            {
                entity.ToTable("UserLicense");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.BackSideFile).HasMaxLength(255);

                entity.Property(e => e.FrontSideFile).HasMaxLength(255);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.UserLicenseCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.UserLicenseUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserLicenseUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.ToTable("Vehicle");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.LicensePlate).HasMaxLength(255);

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.VehicleCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.VehicleUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.VehicleUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.VehicleType)
                    .WithMany(p => p.Vehicles)
                    .HasForeignKey(d => d.VehicleTypeId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Vehicle_VehicleType");
            });

            modelBuilder.Entity<VehicleType>(entity =>
            {
                entity.ToTable("VehicleType");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.VehicleTypeCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.VehicleTypeUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable("Wallet");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.WalletCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.WalletUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.WalletUsers)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.ToTable("WalletTransaction");

                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.BookingDetail)
                    .WithMany(p => p.WalletTransactions)
                    .HasForeignKey(d => d.BookingDetailId)
                    .HasConstraintName("FK_WalletTransaction_BookingDetail");

                entity.HasOne(d => d.Booking)
                    .WithMany(p => p.WalletTransactions)
                    .HasForeignKey(d => d.BookingId)
                    .HasConstraintName("FK_WalletTransaction_Booking");

                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.WalletTransactionCreatedByNavigations)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.UpdatedByNavigation)
                    .WithMany(p => p.WalletTransactionUpdatedByNavigations)
                    .HasForeignKey(d => d.UpdatedBy)
                    .OnDelete(DeleteBehavior.ClientSetNull);

                entity.HasOne(d => d.Wallet)
                    .WithMany(p => p.WalletTransactions)
                    .HasForeignKey(d => d.WalletId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WalletTransaction_Wallet");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
