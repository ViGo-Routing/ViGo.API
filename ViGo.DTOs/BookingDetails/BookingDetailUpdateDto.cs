using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.DTOs.BookingDetails
{
    public class BookingDetailUpdateDto
    {
    }

    public class BookingDetailUpdateStatusDto
    {
        public Guid BookingDetailId { get; set; }
        public BookingDetailStatus Status { get; set; }
        public DateTime? Time { get; set; }
    }
}
