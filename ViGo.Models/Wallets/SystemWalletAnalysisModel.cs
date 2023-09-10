using ViGo.Models.Analysis;

namespace ViGo.Models.Wallets
{
    public class SystemWalletAnalysisModel
    {
        public int TotalTransactions { get; set; }
        public double TotalAmount { get; set; }
        public double TotalCancelFeeAmount { get; set; }
        public double TotalTripPaidAmount { get; set; }
        public double TotalProfits { get; set; }

    }

    public class SystemWalletAnalysises
    {
        public string Time { get; set; }
        public SystemWalletAnalysisModel Analysis { get; set; }
    }

    public class SystemWalletAnalysisRequest
    {
        public AnalysisType AnalysisType { get; set; } = AnalysisType.WEEK;
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
