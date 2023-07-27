using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.Bookings
{
    public class BookingAnalysisModel
    {
        public int TotalBookings { get; set; }
        public int TotalCanceledBookings { get; set; }
        public int TotalCompletedBookings { get; set; }
        public int TotalConfirmedBookings { get; set; }
    }
}
