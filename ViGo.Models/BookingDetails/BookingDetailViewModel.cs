using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;
using ViGo.Models.RouteRoutines;
using ViGo.Models.Routes;
using ViGo.Models.Stations;
using ViGo.Models.Users;

namespace ViGo.Models.BookingDetails
{
    public class BookingDetailViewModel
    {
        public Guid Id { get; set; }
        public Guid? BookingId { get; set; }
        //public Guid? DriverId { get; set; }
        public UserViewModel? Driver { get; set; }
        //public Guid CustomerRouteId { get; set; }
        //public RouteViewModel? CustomerRoute { get; set; }
        //public Guid? DriverRouteId { get; set; }
        //public RouteViewModel? DriverRoute { get; set; }
        public Guid CustomerRouteRoutineId { get; set; }
        public RouteRoutineViewModel CustomerRouteRoutine { get; set; }
        public StationViewModel StartStation { get; set; }
        public StationViewModel EndStation { get; set; }
        public TimeSpan CustomerDesiredPickupTime { get; set; }
        public DateTime? AssignedTime { get; set; }
        public DateTime Date { get; set; }
        public double? Price { get; set; }
        public double? PriceAfterDiscount { get; set; }
        public double? DriverWage { get; set; }
        //public TimeSpan? BeginTime { get; set; }
        public DateTime? ArriveAtPickupTime { get; set; }
        public DateTime? PickupTime { get; set; }
        public DateTime? DropoffTime { get; set; }
        public short? Rate { get; set; }
        public string? Feedback { get; set; }
        public BookingDetailStatus Status { get; set; }
        public BookingDetailType Type { get; set; }
        public Guid? CanceledUserId { get; set; } 
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }

        public BookingDetailViewModel(BookingDetail bookingDetail)
        {
            Id = bookingDetail.Id;
            BookingId = bookingDetail.BookingId;            
            CustomerRouteRoutineId = bookingDetail.CustomerRouteRoutineId;
            CustomerDesiredPickupTime = bookingDetail.CustomerDesiredPickupTime;
            AssignedTime = bookingDetail.AssignedTime;
            Date = bookingDetail.Date;
            Price = bookingDetail.Price;
            PriceAfterDiscount = bookingDetail.PriceAfterDiscount;
            DriverWage = bookingDetail.DriverWage;
            ArriveAtPickupTime = bookingDetail.ArriveAtPickupTime;
            PickupTime = bookingDetail.PickupTime;
            DropoffTime = bookingDetail.DropoffTime;
            Rate = bookingDetail.Rate;
            Feedback = bookingDetail.Feedback;
            Status = bookingDetail.Status;
            CreatedBy = bookingDetail.CreatedBy;
            CreatedTime = bookingDetail.CreatedTime;
            UpdatedTime = bookingDetail.UpdatedTime;
            UpdatedBy = bookingDetail.UpdatedBy;
            CanceledUserId = bookingDetail.CanceledUserId;
            //Driver = driver;
            Type = bookingDetail.Type;
        }

        public BookingDetailViewModel(BookingDetail bookingDetail,
            RouteRoutineViewModel customerRoutine,
            StationViewModel startStation, StationViewModel endStation,
            UserViewModel? driver)
            : this(bookingDetail)
        {
            //Id = bookingDetail.Id;
            Driver = driver;
            //CustomerRouteId = bookingDetail.CustomerRouteId;
            //CustomerRouteRoutineId = bookingDetail.CustomerRouteRoutineId;
            CustomerRouteRoutine = customerRoutine;
            StartStation = startStation;
            EndStation = endStation;
            //CustomerDesiredPickupTime = bookingDetail.CustomerDesiredPickupTime;
            //AssignedTime = bookingDetail.AssignedTime;
            //Date = bookingDetail.Date;
            //Price = bookingDetail.Price;
            //PriceAfterDiscount = bookingDetail.PriceAfterDiscount;
            //DriverWage = bookingDetail.DriverWage;
            //ArriveAtPickupTime = bookingDetail.ArriveAtPickupTime;
            //PickupTime = bookingDetail.PickupTime;
            //DropoffTime = bookingDetail.DropoffTime;
            //Rate = bookingDetail.Rate;
            //Feedback = bookingDetail.Feedback;
            //Status = bookingDetail.Status;
            //CreatedTime = bookingDetail.CreatedTime;
            //CreatedBy = bookingDetail.CreatedBy;
            //UpdatedTime = bookingDetail.UpdatedTime;
            //UpdatedBy = bookingDetail.UpdatedBy;
        }

        //public BookingDetailViewModel(BookingDetail bookingDetail,
        //    UserViewModel? driver,
        //    //RouteViewModel customerRoute,
        //    RouteViewModel driverRoute)
        //    : this(bookingDetail, driver)
        //{
        //    //CustomerRoute = customerRoute;
        //    DriverRoute = driverRoute;
        //}
    }

    public class BookingDetailSortingParameters : SortingParameters
    {
        public BookingDetailSortingParameters()
        {
            OrderBy = QueryStringUtilities.ToSortingCriteria(
                new SortingCriteria(nameof(BookingDetail.Date)),
                new SortingCriteria(nameof(BookingDetail.CustomerDesiredPickupTime)));
        }
    }

    public class BookingDetailFilterParameters
    {
        public DateOnly? MinDate { get; set; }
        public DateOnly? MaxDate { get; set; }
        public TimeOnly? MinPickupTime { get; set; }
        public TimeOnly? MaxPickupTime { get; set; }
        public double? StartLocationLat { get; set; }
        public double? StartLocationLng { get; set; }
        public double? StartLocationRadius { get; set; }
        public double? EndLocationLat { get; set; }
        public double? EndLocationLng { get; set; }
        public double? EndLocationRadius { get; set; }
        public string? Status { get; set; }
    }
}
