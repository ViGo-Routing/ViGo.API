namespace ViGo.Models.Users
{
    public class UserAnalysisModel
    {
        public int TotalActiveUsers { get; set; }
        public int TotalInactiveUsers { get; set; }
        public int TotalBannedUsers { get; set; }
        public int TotalPendingDrivers { get; set; }
        public int TotalRejectedDrivers { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalNewCustomersInCurrentMonth { get; set; }
        public int TotalNewDriversInCurrentMonth { get; set; }

    }

    public class SingleUserAnalysisModel
    {
        public int TotalCompletedTrips { get; set; }
    }
}
