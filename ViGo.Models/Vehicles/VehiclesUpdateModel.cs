﻿namespace ViGo.Models.Vehicles
{
    public class VehiclesUpdateModel
    {
        public string? Name { get; set; } = null!;
        public string? LicensePlate { get; set; } = null!;
        public Guid? VehicleTypeId { get; set; }
        public Guid UserLicenseId { get; set; }

        //public Guid UserId { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
