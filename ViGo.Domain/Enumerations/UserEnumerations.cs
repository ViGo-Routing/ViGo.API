namespace ViGo.Domain.Enumerations
{
    public enum UserRole : short
    {
        ADMIN = 0,
        CUSTOMER = 1,
        DRIVER = 2,
        STAFF = 3
    }

    public enum UserStatus : short
    {
        PENDING = 0,
        ACTIVE = 1,
        INACTIVE = -1,
        BANNED = -3,
        REJECTED = -2
    }

    public enum UserLicenseType : short
    {
        IDENTIFICATION = 1,
        DRIVER_LICENSE = 2,
        VEHICLE_REGISTRATION = 3
    }

    public enum UserLicenseStatus : short
    {
        PENDING = 0,
        APPROVED = 1,
        REJECTED = -1
    }
}
