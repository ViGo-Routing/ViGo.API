namespace ViGo.Models.Wallets
{
    public class SystemWalletAnalysisModel
    {
        public int TotalTransactions { get; set; }
        public double TotalAmount { get; set; }
        public double TotalCancelFeeAmount { get; set; }
        public double TotalTripPaidAmount { get; set; }
    }
}
