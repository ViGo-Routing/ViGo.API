using ViGo.Domain.Enumerations;

namespace ViGo.Models.Users
{
    public class UserRegisterModel
    {
        public string? Name { get; set; }
        public string Phone { get; set; }
        public string? Password { get; set; }
        public string? FirebaseUid { get; set; }
        public UserRole Role { get; set; }
    }
}
