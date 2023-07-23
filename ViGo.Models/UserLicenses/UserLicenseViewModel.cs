using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Domain;
using ViGo.Models.Users;
using ViGo.Models.Vehicles;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;

namespace ViGo.Models.UserLicenses
{
    public class UserLicenseViewModel
    {
        public Guid Id { get; set; }
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
        public UserViewModel User { get; set; } = null!;

        public UserLicenseViewModel(UserLicense userLicense)
        {
            Id = userLicense.Id;
            UserId = userLicense.UserId;
            FrontSideFile = userLicense.FrontSideFile;
            BackSideFile = userLicense.BackSideFile;
            LicenseType = userLicense.LicenseType;
            Status = userLicense.Status;
            CreatedTime = userLicense.CreatedTime;
            CreatedBy = userLicense.CreatedBy;
            UpdatedTime = userLicense.UpdatedTime;
            UpdatedBy = userLicense.UpdatedBy;
        }
        public UserLicenseViewModel(UserLicense userLicense, UserViewModel user)
        {
            Id = userLicense.Id;
            UserId = userLicense.UserId;
            FrontSideFile = userLicense.FrontSideFile;
            BackSideFile = userLicense.BackSideFile;
            LicenseType = userLicense.LicenseType;
            Status = userLicense.Status;
            CreatedTime = userLicense.CreatedTime;
            CreatedBy = userLicense.CreatedBy;
            UpdatedTime = userLicense.UpdatedTime;
            UpdatedBy = userLicense.UpdatedBy;
            User = user;
        }

    }

    public class UserLicenseSortingParameters : SortingParameters
    {
        public UserLicenseSortingParameters()
        {
            OrderBy = QueryStringUtilities.ToSortingCriteria(
                new SortingCriteria(nameof(UserLicense.UserId)),
                new SortingCriteria(nameof(UserLicense.Status)));
        }
    }
}
