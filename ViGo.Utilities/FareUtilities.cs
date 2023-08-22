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

        /// <summary>
        /// Determine if two Fare Distance ranges are overlap
        /// </summary>
        /// <param name="firstRange">First Distance Range</param>
        /// <param name="secondRange">Second Distance Range</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns>False if two Fare Distance ranges are not overlap</returns>
        /// <exception cref="ApplicationException">Throw exception with the error message if two Fare Distance ranges are overlap</exception>
        public static bool IsOverlap(this FareDistanceRange firstRange,
            FareDistanceRange secondRange, string errorMessage)
        {
            if (firstRange.MaxDistance is null)
            {
                if (secondRange.MaxDistance is null)
                {
                    throw new ApplicationException(errorMessage);
                }
                else
                {
                    if (secondRange.MaxDistance > firstRange.MinDistance)
                    {
                        throw new ApplicationException(errorMessage);
                    }
                }

            }
            else
            {
                if (firstRange.MaxDistance > secondRange.MinDistance)
                {
                    throw new ApplicationException(errorMessage);
                }
            }
            return false;
        }
    }

    public class FareDistanceRange
    {
        public double MinDistance { get; set; }
        public double? MaxDistance { get; set; }

        public FareDistanceRange(double minDistance, double? maxDistance)
        {
            MinDistance = minDistance;
            MaxDistance = maxDistance;
        }
    }
}
