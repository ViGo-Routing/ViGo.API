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
        //private static async Task<IEnumerable<FareDiscount>>
        //    GetFareDiscountsAsync(IUnitOfWork)
        public static double RoundToThousands(double fee)
            => Math.Ceiling(fee / 1000) * 1000;

        public static double FloorToHundreds(double fee)
            => Math.Floor(fee / 100) * 100;

        public static double FloorToThousands(double fee)
            => Math.Floor(fee / 1000) * 1000;

        public static double CalculateTripFare(double distance,
            TimeOnly startTime, TimeOnly endTime,
            FareForCalculationModel fare)
        {
            return CalculateFareBasedOnDistance(distance, fare);
        }

        #region Private Members

        #region Fare Calculation
        private static double CalculateFareBasedOnDistance(double distance,
            FareForCalculationModel fare)
        {
            double tripFare = 0;

            if (distance <= fare.MinimumBaseDistance)
            {
                tripFare = fare.MinimumBasePrice;
            } else
            {
                // distance > fare.MinimumBaseDistance
                tripFare = fare.MinimumBasePrice;
                int policyCount = fare.FarePolicies.Count;
                double subDistance = distance - fare.MinimumBaseDistance;

                int policyDistanceIndex = PolicyDistanceFindIndex(distance, fare.MinimumBaseDistance, fare.FarePolicies);
                if (policyDistanceIndex < 0)
                {
                    throw new ApplicationException("Thông tin khoảng cách và giá tiền không hợp lệ!");
                }
                else if (policyDistanceIndex == 0)
                {
                    tripFare += subDistance * fare.FarePolicies[policyDistanceIndex].PricePerKm.Value;
                }
                //else if (policyDistanceIndex == policyCount - 1)
                //{
                    
                //}
                else
                {
                    for (int i = 0; i <= policyDistanceIndex; i++)
                    {
                        FarePolicyForCalculationModel farePolicy = fare.FarePolicies[i];
                        if (farePolicy.MinDistanceBoundary.HasValue
                            && farePolicy.MaxDistanceBoundary.HasValue)
                        {
                            if (i == policyDistanceIndex)
                            {
                                tripFare += subDistance * farePolicy.PricePerKm.Value;
                            }
                            else
                            {
                                tripFare += (farePolicy.MaxDistanceBoundary.Value - farePolicy.MinDistanceBoundary.Value)
                                * farePolicy.PricePerKm.Value;
                                subDistance = distance - farePolicy.MaxDistanceBoundary.Value;
                            }

                        }
                        else if (!farePolicy.MinDistanceBoundary.HasValue
                            && farePolicy.MaxDistanceBoundary.HasValue)
                        {
                            tripFare += (farePolicy.MaxDistanceBoundary.Value - fare.MinimumBaseDistance)
                                * farePolicy.PricePerKm.Value;
                            subDistance = distance - farePolicy.MaxDistanceBoundary.Value;
                        }
                        else if (farePolicy.MinDistanceBoundary.HasValue
                            && !farePolicy.MaxDistanceBoundary.HasValue)
                        {
                            // The last one
                            tripFare += (distance - farePolicy.MinDistanceBoundary.Value)
                                * farePolicy.PricePerKm.Value;
                        }
                    }
                }
            }

            return tripFare;
        }

        private static int PolicyDistanceFindIndex(double distance,
            double baseDistance,
            IList<FarePolicyForCalculationModel> farePolicies)
        {
            int policyCount = farePolicies.Count;
            if (distance > baseDistance
                && distance < farePolicies[0].MaxDistanceBoundary)
            {
                return 0;
            }
            if (distance > farePolicies[policyCount - 1].MinDistanceBoundary)
            {
                return policyCount - 1;
            }
            for (int i = 1; i <= policyCount - 2; i++)
            {
                if (distance >= farePolicies[i].MinDistanceBoundary
                    && distance < farePolicies[i].MaxDistanceBoundary)
                {
                    return i;
                }
            }
            return -1;
        }
        #endregion

        #region Time
        private static TimeOnly NightTripMinBoundary = new TimeOnly(22, 00, 00);
        private static TimeOnly NextDayNightBoundary = new TimeOnly(23, 59, 59, 999);
        private static TimeOnly NextDayMorningBoundary = new TimeOnly(0, 0, 0, 0);
        private static TimeOnly NightTripMaxBoundary = new TimeOnly(5, 59, 59, 999);

        private static bool IsNightTrip(TimeOnly startTime,
            TimeOnly endTime)
        {
            if ((startTime.IsBetween(NightTripMinBoundary, NextDayNightBoundary)
                && endTime.IsBetween(NightTripMinBoundary, NextDayNightBoundary))
                || (startTime.IsBetween(NextDayMorningBoundary, NightTripMaxBoundary)
                && endTime.IsBetween(NextDayMorningBoundary, NightTripMaxBoundary)))
            {
                // StartTime and EndTime is within a day
                if (startTime > endTime)
                {
                    (startTime, endTime) = (endTime, startTime);
                }
            }

            if (IsNightTime(startTime)
                || (startTime < NightTripMinBoundary && endTime >= NightTripMinBoundary.AddMinutes(10)))
            {
                return true;
            }

            return false;
        }

        private static bool IsNightTime(TimeOnly timeTocheck)
        {
            return timeTocheck.IsBetween(NightTripMinBoundary, NextDayNightBoundary)
                || timeTocheck.IsBetween(NextDayMorningBoundary, NightTripMaxBoundary);
        }
        #endregion
        #endregion
    }


}
