using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class User
    {
        public User()
        {
            BookingDetails = new HashSet<BookingDetail>();
            Bookings = new HashSet<Booking>();
            Notifications = new HashSet<Notification>();
            Reports = new HashSet<Report>();
            Routes = new HashSet<Route>();
            UserLicenses = new HashSet<UserLicense>();
            Vehicles = new HashSet<Vehicle>();
            Wallets = new HashSet<Wallet>();
        }

        public override Guid Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Password { get; set; } = null!;
        public string? Name { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockedOutStart { get; set; }
        public DateTime? LockedOutEnd { get; set; }

        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
        public virtual ICollection<Route> Routes { get; set; }
        public virtual ICollection<UserLicense> UserLicenses { get; set; }
        public virtual ICollection<Vehicle> Vehicles { get; set; }
        public virtual ICollection<Wallet> Wallets { get; set; }
    }
}
