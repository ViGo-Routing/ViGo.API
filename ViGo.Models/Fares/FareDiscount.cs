using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.Fares
{
    public class FareDiscount
    {
        public short NumberOfTickets { get; set; }
        public double Discount { get; set; }

        public FareDiscount(short numberOfTickets, double discount)
        {
            NumberOfTickets = numberOfTickets;
            Discount = discount;
        }
    }
}
