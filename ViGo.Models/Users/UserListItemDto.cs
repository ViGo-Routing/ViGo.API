using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Users
{
    public class UserListItemDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        //public string Password { get; set; } = null!;
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
        //public bool IsLockedOut { get; set; }
        //public DateTime? LockedOutStart { get; set; }
        //public DateTime? LockedOutEnd { get; set; }

        public UserListItemDto(User user)
        {
            Id = user.Id;
            Email = user.Email;
            Phone = user.Phone;
            Name = user.Name;
            Gender = user.Gender;
            DateOfBirth = user.DateOfBirth;
            AvatarUrl = user.AvatarUrl;
            Role = user.Role;
            Status = user.Status;
            CreatedTime = user.CreatedTime;
            CreatedBy = user.CreatedBy;
            UpdatedTime = user.UpdatedTime;
            UpdatedBy = user.UpdatedBy;
        }
    }
}
