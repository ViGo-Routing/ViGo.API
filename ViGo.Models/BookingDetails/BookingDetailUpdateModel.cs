using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.BookingDetails
{
    public class BookingDetailUpdateModel
    {
    }

    public class BookingDetailUpdateStatusModel
    {
        public Guid BookingDetailId { get; set; }
        public BookingDetailStatus Status { get; set; }
        public DateTime? Time { get; set; }
    }

    public class BookingDetailAssignDriverModel
    {
        public Guid BookingDetailId { get; set; }
        public Guid DriverId { get; set; }

    }
}
