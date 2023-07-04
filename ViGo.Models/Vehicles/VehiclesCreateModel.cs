﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.Users;

namespace ViGo.Models.Vehicles
{
    public class VehiclesCreateModel
    {
        public string Name { get; set; } = null!;
        public string LicensePlate { get; set; } = null!;
        public Guid VehicleTypeId { get; set; }
        public Guid UserId { get; set; }
        public Guid UserLicenseId { get; set; }

    }
}
