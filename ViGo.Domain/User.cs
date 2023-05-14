using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class User
    {
        public User()
        {
            BookingCreatedByNavigations = new HashSet<Booking>();
            BookingCustomers = new HashSet<Booking>();
            BookingDetailCreatedByNavigations = new HashSet<BookingDetail>();
            BookingDetailDrivers = new HashSet<BookingDetail>();
            BookingDetailUpdatedByNavigations = new HashSet<BookingDetail>();
            BookingUpdatedByNavigations = new HashSet<Booking>();
            FareCreatedByNavigations = new HashSet<Fare>();
            FarePolicyCreatedByNavigations = new HashSet<FarePolicy>();
            FarePolicyUpdatedByNavigations = new HashSet<FarePolicy>();
            FareUpdatedByNavigations = new HashSet<Fare>();
            InverseCreatedByNavigation = new HashSet<User>();
            InverseUpdatedByNavigation = new HashSet<User>();
            NotificationCreatedByNavigations = new HashSet<Notification>();
            NotificationUpdatedByNavigations = new HashSet<Notification>();
            NotificationUsers = new HashSet<Notification>();
            PromotionCreatedByNavigations = new HashSet<Promotion>();
            PromotionUpdatedByNavigations = new HashSet<Promotion>();
            ReportCreatedByNavigations = new HashSet<Report>();
            ReportUpdatedByNavigations = new HashSet<Report>();
            ReportUsers = new HashSet<Report>();
            RouteCreatedByNavigations = new HashSet<Route>();
            RouteRoutineCreatedByNavigations = new HashSet<RouteRoutine>();
            RouteRoutineUpdatedByNavigations = new HashSet<RouteRoutine>();
            RouteRoutineUsers = new HashSet<RouteRoutine>();
            RouteStationCreatedByNavigations = new HashSet<RouteStation>();
            RouteStationUpdatedByNavigations = new HashSet<RouteStation>();
            RouteUpdatedByNavigations = new HashSet<Route>();
            RouteUsers = new HashSet<Route>();
            StationCreatedByNavigations = new HashSet<Station>();
            StationUpdatedByNavigations = new HashSet<Station>();
            UserLicenseCreatedByNavigations = new HashSet<UserLicense>();
            UserLicenseUpdatedByNavigations = new HashSet<UserLicense>();
            UserLicenseUsers = new HashSet<UserLicense>();
            VehicleCreatedByNavigations = new HashSet<Vehicle>();
            VehicleTypeCreatedByNavigations = new HashSet<VehicleType>();
            VehicleTypeUpdatedByNavigations = new HashSet<VehicleType>();
            VehicleUpdatedByNavigations = new HashSet<Vehicle>();
            VehicleUsers = new HashSet<Vehicle>();
            WalletCreatedByNavigations = new HashSet<Wallet>();
            WalletTransactionCreatedByNavigations = new HashSet<WalletTransaction>();
            WalletTransactionUpdatedByNavigations = new HashSet<WalletTransaction>();
            WalletUpdatedByNavigations = new HashSet<Wallet>();
            WalletUsers = new HashSet<Wallet>();
        }

        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; } = null!;
        public string? Name { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Photo { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTimeOffset? LockedOutStart { get; set; }
        public DateTimeOffset? LockedOutEnd { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual ICollection<Booking> BookingCreatedByNavigations { get; set; }
        public virtual ICollection<Booking> BookingCustomers { get; set; }
        public virtual ICollection<BookingDetail> BookingDetailCreatedByNavigations { get; set; }
        public virtual ICollection<BookingDetail> BookingDetailDrivers { get; set; }
        public virtual ICollection<BookingDetail> BookingDetailUpdatedByNavigations { get; set; }
        public virtual ICollection<Booking> BookingUpdatedByNavigations { get; set; }
        public virtual ICollection<Fare> FareCreatedByNavigations { get; set; }
        public virtual ICollection<FarePolicy> FarePolicyCreatedByNavigations { get; set; }
        public virtual ICollection<FarePolicy> FarePolicyUpdatedByNavigations { get; set; }
        public virtual ICollection<Fare> FareUpdatedByNavigations { get; set; }
        public virtual ICollection<User> InverseCreatedByNavigation { get; set; }
        public virtual ICollection<User> InverseUpdatedByNavigation { get; set; }
        public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; }
        public virtual ICollection<Notification> NotificationUpdatedByNavigations { get; set; }
        public virtual ICollection<Notification> NotificationUsers { get; set; }
        public virtual ICollection<Promotion> PromotionCreatedByNavigations { get; set; }
        public virtual ICollection<Promotion> PromotionUpdatedByNavigations { get; set; }
        public virtual ICollection<Report> ReportCreatedByNavigations { get; set; }
        public virtual ICollection<Report> ReportUpdatedByNavigations { get; set; }
        public virtual ICollection<Report> ReportUsers { get; set; }
        public virtual ICollection<Route> RouteCreatedByNavigations { get; set; }
        public virtual ICollection<RouteRoutine> RouteRoutineCreatedByNavigations { get; set; }
        public virtual ICollection<RouteRoutine> RouteRoutineUpdatedByNavigations { get; set; }
        public virtual ICollection<RouteRoutine> RouteRoutineUsers { get; set; }
        public virtual ICollection<RouteStation> RouteStationCreatedByNavigations { get; set; }
        public virtual ICollection<RouteStation> RouteStationUpdatedByNavigations { get; set; }
        public virtual ICollection<Route> RouteUpdatedByNavigations { get; set; }
        public virtual ICollection<Route> RouteUsers { get; set; }
        public virtual ICollection<Station> StationCreatedByNavigations { get; set; }
        public virtual ICollection<Station> StationUpdatedByNavigations { get; set; }
        public virtual ICollection<UserLicense> UserLicenseCreatedByNavigations { get; set; }
        public virtual ICollection<UserLicense> UserLicenseUpdatedByNavigations { get; set; }
        public virtual ICollection<UserLicense> UserLicenseUsers { get; set; }
        public virtual ICollection<Vehicle> VehicleCreatedByNavigations { get; set; }
        public virtual ICollection<VehicleType> VehicleTypeCreatedByNavigations { get; set; }
        public virtual ICollection<VehicleType> VehicleTypeUpdatedByNavigations { get; set; }
        public virtual ICollection<Vehicle> VehicleUpdatedByNavigations { get; set; }
        public virtual ICollection<Vehicle> VehicleUsers { get; set; }
        public virtual ICollection<Wallet> WalletCreatedByNavigations { get; set; }
        public virtual ICollection<WalletTransaction> WalletTransactionCreatedByNavigations { get; set; }
        public virtual ICollection<WalletTransaction> WalletTransactionUpdatedByNavigations { get; set; }
        public virtual ICollection<Wallet> WalletUpdatedByNavigations { get; set; }
        public virtual ICollection<Wallet> WalletUsers { get; set; }
    }
}
