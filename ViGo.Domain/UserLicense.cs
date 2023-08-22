using Newtonsoft.Json;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class UserLicense
    {
        public UserLicense()
        {
            Vehicles = new HashSet<Vehicle>();
        }

        public override Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? FrontSideFile { get; set; }
        public string? BackSideFile { get; set; }
        public UserLicenseType LicenseType { get; set; }
        public UserLicenseStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<Vehicle> Vehicles { get; set; }
    }
}
