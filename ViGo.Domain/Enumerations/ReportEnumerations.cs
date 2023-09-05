namespace ViGo.Domain.Enumerations
{
    public enum ReportStatus : short
    {
        PENDING = 0,
        PROCESSED = 1,
        DENIED = -1
    }

    public enum ReportType : short
    {
        DRIVER_NOT_COMING = 0,
        BOOKER_NOT_COMING = 1,
        OTHER = 2,
        DRIVER_CANCEL_TRIP = 3
    }
}
