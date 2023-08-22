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
