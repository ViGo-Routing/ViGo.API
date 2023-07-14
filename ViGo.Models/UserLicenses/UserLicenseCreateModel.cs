using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.UserLicenses
{
    public class UserLicenseCreateModel
    {
        //public Guid UserId { get; set; }
        public string? FrontSideFile { get; set; }
        public string? BackSideFile { get; set; }
        public UserLicenseType LicenseType { get; set; }

    }
}
