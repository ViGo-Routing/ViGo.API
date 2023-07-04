using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.UserLicenses
{
    public class UserLicenseUpdateModel
    {
        public UserLicenseStatus Status { get; set; }
        public bool? IsDeleted { get; set; }

    }
}
