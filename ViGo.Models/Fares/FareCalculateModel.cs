using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Fares
{
    public class FareCalculateRequestModel
    {
        public Guid VehicleTypeId { get; set; }
        public TimeOnly BeginTime { get; set; }
        public double Duration { get; set; }
        public double Distance { get; set; }
        public int TotalNumberOfTickets { get; set; }
        public RoutineType? RoutineType { get; set; }
    }

    public class FareCalculateResponseModel
    {
        public double OriginalFare { get; set; }
        public double AdditionalFare { get; set; }
        public double NumberTicketsDiscount { get; set; }
        public double RouteTypeDiscount { get; set; }
        public double FinalFare { get; set; }
    }
}
