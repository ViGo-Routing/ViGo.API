using ViGo.Domain.Enumerations;

namespace ViGo.Models.UserLicenses
{
    public class UserLicenseUpdateModel
    {
        public UserLicenseStatus Status { get; set; }
        public bool? IsDeleted { get; set; }

    }
}
