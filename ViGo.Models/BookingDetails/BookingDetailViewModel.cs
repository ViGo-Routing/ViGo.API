using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Routes;
using ViGo.Models.Users;

namespace ViGo.Models.BookingDetails
{
    public class BookingDetailViewModel
    {
        public Guid Id { get; set; }
        public Guid? BookingId { get; set; }
        public Guid? DriverId { get; set; }
        //public UserViewModel? Driver { get; set; }
        public Guid CustomerRouteId { get; set; }
        //public RouteViewModel? CustomerRoute { get; set; }
        public Guid? DriverRouteId { get; set; }
        //public RouteViewModel? DriverRoute { get; set; }
        public DateTime? AssignedTime { get; set; }
        public DateTime? Date { get; set; }
        public double? Price { get; set; }
        public double? PriceAfterDiscount { get; set; }
        public double? DriverWage { get; set; }
        public TimeSpan? BeginTime { get; set; }
        public DateTime? ArriveAtPickupTime { get; set; }
        public DateTime? PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }
        public short? Rate { get; set; }
        public string? Feedback { get; set; }
        public BookingDetailStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }

        public BookingDetailViewModel(BookingDetail bookingDetail)
        {
            Id = bookingDetail.Id;
            //Driver = driver;
            DriverId = bookingDetail.DriverId;
            CustomerRouteId = bookingDetail.CustomerRouteId;
            DriverRouteId = bookingDetail.DriverRouteId;
            AssignedTime = bookingDetail.AssignedTime;
            Date = bookingDetail.Date;
            Price = bookingDetail.Price;
            PriceAfterDiscount = bookingDetail.PriceAfterDiscount;
            DriverWage = bookingDetail.DriverWage;
            BeginTime = bookingDetail.BeginTime;
            ArriveAtPickupTime = bookingDetail.ArriveAtPickupTime;
            PickupTime = bookingDetail.PickupTime;
            DropoffTime = bookingDetail.DropoffTime;
            Rate = bookingDetail.Rate;
            Feedback = bookingDetail.Feedback;
            Status = bookingDetail.Status;
            CreatedTime = bookingDetail.CreatedTime;
            CreatedBy = bookingDetail.CreatedBy;
            UpdatedTime = bookingDetail.UpdatedTime;
            UpdatedBy = bookingDetail.UpdatedBy;
        }
        
        //public BookingDetailViewModel(BookingDetail bookingDetail,
        //    UserViewModel? driver,
        //    RouteViewModel customerRoute,
        //    RouteViewModel driverRoute)
        //    : this(bookingDetail, driver)
        //{
        //    CustomerRoute = customerRoute;
        //    DriverRoute = driverRoute;
        //}
    }
}
