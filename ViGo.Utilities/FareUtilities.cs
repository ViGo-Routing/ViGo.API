using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Models.Fares;

namespace ViGo.Utilities
{
    public static class FareUtilities
    {
        public const double DriverWagePercent = 0.65;

        //private static async Task<IEnumerable<FareDiscount>>
        //    GetFareDiscountsAsync(IUnitOfWork)
        public static double RoundToThousands(double fee)
            => Math.Ceiling(fee / 1000) * 1000;

        public static double FloorToHundreds(double fee)
            => Math.Floor(fee / 100) * 100;

        public static double FloorToThousands(double fee)
            => Math.Floor(fee / 1000) * 1000;

        //public static double CalculateTripFare(double distance,
        //    TimeOnly startTime, TimeOnly endTime,
        //    FareForCalculationModel fare)
        //{
        //    return CalculateFareBasedOnDistance(distance, fare);
        //}

    }


}
