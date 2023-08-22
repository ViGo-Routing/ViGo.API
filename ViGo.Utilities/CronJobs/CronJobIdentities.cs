namespace ViGo.Utilities.CronJobs
{
    public static class CronJobIdentities
    {
        public static readonly string UPCOMING_TRIP_NOTIFICATION_JOBKEY = "UpcomingTripNotification";
        public static readonly string UPCOMING_TRIP_NOTIFICATION_TRIGGER_ID = "UpcomingTripNotification_Trigger";
        public static readonly string CHECK_TRANSACTION_STATUS_JOBKEY = "CheckTransactionStatus";
        public static readonly string CHECK_TRANSACTION_STATUS_TRIGGER_ID = "CheckTransactionStatus_Trigger";
        public static readonly string RESET_WEEKLY_CANCEL_RATE_JOBKEY = "ResetWeeklyCancelRate";
        public static readonly string RESET_WEEKLY_CANCEL_RATE_TRIGGER_ID = "ResetWeeklyCancelRate_Trigger";

        public static readonly string SCHEDULER_ID = "ViGo-Scheduler";

        public static readonly string BOOKING_DETAIL_ID_JOB_DATA = "BookingDetailId";
        public static readonly string TRANSACTION_ID_JOB_DATA = "TransactionId";
        public static readonly string CLIENT_IP_ADDRESS_JOB_DATA = "TransactionId";
    }
}
