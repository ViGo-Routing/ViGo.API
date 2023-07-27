using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.BookingDetails
{
    public class BookingDetailAnalysisModel
    {
        public int TotalBookingDetails { get; set; }
        public int TotalCanceledBookingDetails { get; set; }
        public int TotalCanceledByCustomerBookingDetails { get; set; }
        public int TotalCanceledByDriverBookingDetails { get; set; }
        public int TotalCompletedBookingDetails { get; set; }
        public int TotalPendingPaidBookingDetails { get; set; }
        public int TotalAssignedBookingDetails { get; set; }
        public int TotalUnassignedBookingDetails { get; set; }
    }
}
