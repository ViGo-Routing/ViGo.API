using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.UserLicenses;
using ViGo.Models.Users;
using ViGo.Models.VehicleTypes;

namespace ViGo.Models.Vehicles
{
    public class VehiclesViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public Guid VehicleTypeId { get; set; }
        public Guid UserId { get; set; }
        public Guid UserLicenseId { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public UserViewModel User { get; set; }
        public VehicleTypeViewModel VehicleType { get; set; }
        public UserLicenseViewModel UserLicense { get; set; } = null!;


        public VehiclesViewModel(Vehicle vehicle, UserViewModel user, VehicleTypeViewModel vehicleType, UserLicenseViewModel userLicense) {
            Id = vehicle.Id;
            Name = vehicle.Name;
            LicensePlate = vehicle.LicensePlate;
            VehicleTypeId = vehicle.VehicleTypeId;
            UserId = vehicle.UserId;
            CreatedBy = vehicle.CreatedBy;
            UpdatedTime = vehicle.UpdatedTime;
            UpdatedBy = vehicle.UpdatedBy;
            IsDeleted = vehicle.IsDeleted;
            VehicleType = vehicleType;
            User = user;
            UserLicense = userLicense;
        }
    }
}
