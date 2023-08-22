namespace ViGo.DTOs.Users
{
    public class UserUpdateModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
        public bool? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class UserUpdateFcmTokenModel
    {
        public Guid Id { get; set; }
        public string FcmToken { get; set; }
    }
}
