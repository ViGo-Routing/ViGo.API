using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;
using ViGo.Models.Routes;
using ViGo.Models.Stations;
using ViGo.Models.Users;

namespace ViGo.Models.Bookings
{
    public class BookingViewModel
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        //public Guid StartRouteStationId { get; set; }
        //public Guid EndRouteStationId { get; set; }
        public UserViewModel Customer { get; set; }
        //public RouteStationViewModel StartRouteStation { get; set; }
        //public RouteStationViewModel EndRouteStation { get; set; }
        public Guid CustomerRouteId { get; set; }
        public RouteViewModel? CustomerRoute { get; set; }
        //public TimeSpan? StartTime { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? DaysOfWeek { get; set; }
        public double? TotalPrice { get; set; }
        public double? PriceAfterDiscount { get; set; }
        //public PaymentMethod? PaymentMethod { get; set; }
        public bool IsShared { get; set; }
        public double Duration { get; set; }
        public double Distance { get; set; }
        //public Guid? PromotionId { get; set; }
        public Guid VehicleTypeId { get; set; }
        public string VehicleName { get; set; }
        public BookingType Type { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int? TotalBookingDetailsCount { get; set; }
        public int? TotalAssignedBookingDetailsCount { get; set; }
        public int? TotalCompletedBookingDetailsCount { get; set; }

        //public IEnumerable<BookingDetailViewModel> BookingDetails { get; set; }

        public BookingViewModel(Booking booking, UserViewModel customer,
            VehicleType vehicleType)
        {
            Id = booking.Id;
            CustomerId = booking.CustomerId;
            Customer = customer;
            //StartRouteStation = startRouteStation;
            //EndRouteStation = endRouteStation;
            CustomerRoute = null;
            CustomerRouteId = booking.CustomerRouteId;
            StartDate = booking.StartDate;
            EndDate = booking.EndDate;
            DaysOfWeek = booking.DaysOfWeek;
            TotalPrice = booking.TotalPrice;
            PriceAfterDiscount = booking.PriceAfterDiscount;
            //PaymentMethod = booking.PaymentMethod;
            IsShared = booking.IsShared;
            Duration = booking.Duration;
            Distance = booking.Distance;
            //PromotionId = booking.PromotionId;
            VehicleTypeId = booking.VehicleTypeId;
            VehicleName = vehicleType.Name.Trim() + " - " +
                vehicleType.Slot + " chỗ";
            Type = booking.Type;
            Status = booking.Status;
            CreatedTime = booking.CreatedTime;
            CreatedBy = booking.CreatedBy;
            UpdatedTime = booking.UpdatedTime;
            UpdatedBy = booking.UpdatedBy;
            IsDeleted = booking.IsDeleted;
        }

        public BookingViewModel(Booking booking, UserViewModel customer,
            VehicleType vehicleType, int totalBookingDetailsCount,
            int totalAssignedBookingDetailsCount) : this(booking, customer, vehicleType)
        {

            TotalBookingDetailsCount = totalBookingDetailsCount;
            TotalAssignedBookingDetailsCount = totalAssignedBookingDetailsCount;
        }

        public BookingViewModel(Booking booking, UserViewModel customer,
            Route customerRoute,
            StationViewModel startRouteStation,
            StationViewModel endRouteStation,
            VehicleType vehicleType) : this(booking, customer, vehicleType)
        {
            //Id = booking.Id;
            //Customer = customer;
            ////StartRouteStation = startRouteStation;
            ////EndRouteStation = endRouteStation;
            CustomerRoute = new RouteViewModel(customerRoute, startRouteStation, endRouteStation, null, null);
            //StartDate = booking.StartDate;
            //EndDate = booking.EndDate;
            //DaysOfWeek = booking.DaysOfWeek;
            //TotalPrice = booking.TotalPrice;
            //PriceAfterDiscount = booking.PriceAfterDiscount;
            ////PaymentMethod = booking.PaymentMethod;
            //IsShared = booking.IsShared;
            //Duration = booking.Duration;
            //Distance = booking.Distance;
            //PromotionId = booking.PromotionId;
            //VehicleTypeId = booking.VehicleTypeId;
            //VehicleName = vehicleType.Name + " - " + 
            //    vehicleType.Slot + " chỗ";
            //Type = booking.Type;
            //Status = booking.Status;
            //CreatedTime = booking.CreatedTime;
            //CreatedBy = booking.CreatedBy;
            //UpdatedTime = booking.UpdatedTime;
            //UpdatedBy = booking.UpdatedBy;
            //IsDeleted = booking.IsDeleted;
        }
        public BookingViewModel(Booking booking, UserViewModel customer,
            Route customerRoute,
            StationViewModel startRouteStation,
            StationViewModel endRouteStation,
            VehicleType vehicleType, int totalBookingDetailsCount,
            int totalAssignedBookingDetailsCount, int totalCompletedBookingDetailsCount)
            : this(booking, customer, customerRoute, startRouteStation, endRouteStation, vehicleType)
        {
            TotalBookingDetailsCount = totalBookingDetailsCount;
            TotalAssignedBookingDetailsCount = totalAssignedBookingDetailsCount;
            TotalCompletedBookingDetailsCount = totalCompletedBookingDetailsCount;
        }

        //public BookingViewModel(Booking booking, UserViewModel customer,
        //    RouteStationViewModel startRouteStation,
        //    RouteStationViewModel endRouteStation,
        //    VehicleType vehicleType,
        //    IEnumerable<BookingDetailViewModel> bookingDetails)
        //    : this (booking, customer, startRouteStation, endRouteStation, vehicleType)
        //{
        //    BookingDetails = bookingDetails;
        //}
    }

    public class BookingSortingParameters : SortingParameters
    {
        public BookingSortingParameters()
        {
            OrderBy = QueryStringUtilities.ToSortingCriteria(
                new SortingCriteria(nameof(Booking.CreatedTime), SortingType.DESC),
                new SortingCriteria(nameof(Booking.StartDate)));
        }
    }

    public class BookingFilterParameters
    {
        public DateOnly? MinStartDate { get; set; }
        public DateOnly? MaxStartDate { get; set; }
        public DateOnly? MinEndDate { get; set; }
        public DateOnly? MaxEndDate { get; set; }
    }
}
