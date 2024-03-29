﻿using Newtonsoft.Json;
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
            CanceledBookingDetails = new HashSet<BookingDetail>();
        }

        public override Guid Id { get; set; }
        public string? FirebaseUid { get; set; }
        public string? FcmToken { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public UserRole Role { get; set; }

        public double? Rating { get; set; }
        public double CanceledTripRate { get; set; }
        public double WeeklyCanceledTripRate { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockedOutStart { get; set; }
        public DateTime? LockedOutEnd { get; set; }
        public short FailedLoginCount { get; set; }

        [JsonIgnore]
        public virtual ICollection<BookingDetail> BookingDetails { get; set; }
        [JsonIgnore]
        public virtual ICollection<Booking> Bookings { get; set; }
        [JsonIgnore]
        public virtual ICollection<Notification> Notifications { get; set; }
        [JsonIgnore]
        public virtual ICollection<Report> Reports { get; set; }
        [JsonIgnore]
        public virtual ICollection<Route> Routes { get; set; }
        [JsonIgnore]
        public virtual ICollection<UserLicense> UserLicenses { get; set; }
        [JsonIgnore]
        public virtual ICollection<Vehicle> Vehicles { get; set; }
        [JsonIgnore]
        public virtual ICollection<Wallet> Wallets { get; set; }
        [JsonIgnore]
        public virtual ICollection<BookingDetail> CanceledBookingDetails { get; set; }
    }
}
