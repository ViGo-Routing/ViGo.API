using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Fares;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;

namespace ViGo.Services
{
    public class FareServices : BaseServices
    {
        public FareServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<double> CalculateDriverWage(double bookingDetailFare,
            CancellationToken cancellationToken)
        {
            Setting? driverWageSetting = await work.Settings.GetAsync(
                s => s.Key.Equals(SettingKeys.DriverWagePercent), cancellationToken: cancellationToken);

            if (driverWageSetting is null)
            {
                throw new ApplicationException("Chiết khấu chuyến đi dành cho tài xế chưa được thiết lập!!");
            }

            double percent = double.Parse(driverWageSetting.Value);
            return FareUtilities.RoundToThousands(bookingDetailFare * percent);
        }

        public async Task<FareCalculateResponseModel> CalculateFareBasedOnDistance(
            FareCalculateRequestModel fareModel, CancellationToken cancellationToken)
        {
            if (fareModel.TripType == BookingType.ROUND_TRIP && fareModel.RoundTripBeginTime is null)
            {
                throw new ApplicationException("Thời gian cho chuyến đi khứ hồi chưa được thiết lập!!!");
            }

            Fare? fare = await work.Fares.GetAsync(f => f.VehicleTypeId.Equals(fareModel.VehicleTypeId));
            if (fare is null)
            {
                throw new ApplicationException("Loại phương tiện chưa được cấu hình chính sách tính giá!");
            }
            IEnumerable<FarePolicy> farePolicies = await work.FarePolicies
                .GetAllAsync(query => query.Where(
                    fp => fp.FareId.Equals(fare.Id)));

            FareForCalculationModel fareCalculationModel = new FareForCalculationModel(fare, farePolicies.ToList());

            FareCalculateResponseModel responseModel = new FareCalculateResponseModel
            {
                AdditionalFare = 0,
                FinalFare = 0,
                NumberTicketsDiscount = 0,
                OriginalFare = 0,
                RouteTypeDiscount = 0
            };

            if (fareModel.Distance <= fareCalculationModel.MinimumBaseDistance)
            {
                responseModel.OriginalFare = fareCalculationModel.MinimumBasePrice;
                //responseModel.FinalFare = fareCalculationModel.MinimumBasePrice;
            }
            else
            {
                // distance > fare.MinimumBaseDistance
                responseModel.OriginalFare = fareCalculationModel.MinimumBasePrice;
                //responseModel.FinalFare = fareCalculationModel.MinimumBasePrice;

                int policyCount = fareCalculationModel.FarePolicies.Count;
                double subDistance = fareModel.Distance - fareCalculationModel.MinimumBaseDistance;

                int policyDistanceIndex = PolicyDistanceFindIndex(fareModel.Distance, fareCalculationModel.MinimumBaseDistance, fareCalculationModel.FarePolicies);
                if (policyDistanceIndex < 0)
                {
                    throw new ApplicationException("Thông tin khoảng cách và giá tiền không hợp lệ!");
                }
                else if (policyDistanceIndex == 0)
                {
                    responseModel.OriginalFare += subDistance * fareCalculationModel.FarePolicies[policyDistanceIndex].PricePerKm.Value;
                    //responseModel.FinalFare += subDistance * fareCalculationModel.FarePolicies[policyDistanceIndex].PricePerKm.Value;
                }
                //else if (policyDistanceIndex == policyCount - 1)
                //{

                //}
                else
                {
                    for (int i = 0; i <= policyDistanceIndex; i++)
                    {
                        FarePolicyForCalculationModel farePolicy = fareCalculationModel.FarePolicies[i];
                        if (farePolicy.MinDistanceBoundary.HasValue
                            && farePolicy.MaxDistanceBoundary.HasValue)
                        {
                            if (i == policyDistanceIndex)
                            {
                                responseModel.OriginalFare += subDistance * farePolicy.PricePerKm.Value;
                                responseModel.FinalFare += subDistance * farePolicy.PricePerKm.Value;
                            }
                            else
                            {
                                responseModel.OriginalFare += (farePolicy.MaxDistanceBoundary.Value - farePolicy.MinDistanceBoundary.Value)
                                    * farePolicy.PricePerKm.Value;
                                //responseModel.FinalFare = (farePolicy.MaxDistanceBoundary.Value - farePolicy.MinDistanceBoundary.Value)
                                //    * farePolicy.PricePerKm.Value;
                                subDistance = fareModel.Distance - farePolicy.MaxDistanceBoundary.Value;
                            }

                        }
                        else if (!farePolicy.MinDistanceBoundary.HasValue
                            && farePolicy.MaxDistanceBoundary.HasValue)
                        {
                            responseModel.OriginalFare += (farePolicy.MaxDistanceBoundary.Value - fareCalculationModel.MinimumBaseDistance)
                                * farePolicy.PricePerKm.Value;
                            subDistance = fareModel.Distance - farePolicy.MaxDistanceBoundary.Value;
                        }
                        else if (farePolicy.MinDistanceBoundary.HasValue
                            && !farePolicy.MaxDistanceBoundary.HasValue)
                        {
                            // The last one
                            responseModel.OriginalFare += (fareModel.Distance - farePolicy.MinDistanceBoundary.Value)
                                * farePolicy.PricePerKm.Value;
                        }
                    }
                }
            }

            if (fareModel.TripType == BookingType.ROUND_TRIP)
            {
                responseModel.OriginalFare *= 2;
            }

            //responseModel.FinalFare = responseModel.OriginalFare;
            TimeSpan endTimeSpan = DateTimeUtilities.CalculateTripEndTime(fareModel.BeginTime.ToTimeSpan(), fareModel.Duration);
            TimeOnly endTime = TimeOnly.FromTimeSpan(endTimeSpan);

            if (IsNightTrip(fareModel.BeginTime, endTime))
            {
                // Additional fare
                VehicleType? vehicleType = await work.VehicleTypes.GetAsync(fareModel.VehicleTypeId, 
                    cancellationToken: cancellationToken);
                if (vehicleType is null)
                {
                    throw new ApplicationException("Thông tin phương tiện di chuyển không hợp lệ!!");
                }
                Setting? settingNightTrip = null;
                switch (vehicleType.Type)
                {
                    case VehicleSubType.VI_RIDE:
                        settingNightTrip = await work.Settings.GetAsync(
                            s => s.Key.Equals(SettingKeys.NightTripExtraFeeBike), 
                            cancellationToken: cancellationToken);
                        break;
                    case VehicleSubType.VI_CAR:
                        settingNightTrip = await work.Settings.GetAsync(
                            s => s.Key.Equals(SettingKeys.NightTripExtraFeeCar),
                            cancellationToken: cancellationToken);
                        break;

                }
                if (settingNightTrip != null)
                {
                    responseModel.AdditionalFare += double.Parse(settingNightTrip.Value);

                    //responseModel.OriginalFare += responseModel.AdditionalFare;
                    //responseModel.FinalFare += responseModel.AdditionalFare;
                }
                
            }

            if (fareModel.TripType == BookingType.ROUND_TRIP)
            {
                TimeSpan roundTripEndTimeSpan = DateTimeUtilities.CalculateTripEndTime(
                    fareModel.RoundTripBeginTime.Value.ToTimeSpan(), fareModel.Duration);

                TimeOnly roundTripEndTime = TimeOnly.FromTimeSpan(endTimeSpan);

                if (IsNightTrip(fareModel.RoundTripBeginTime.Value, roundTripEndTime))
                {
                    // Additional fare
                    VehicleType? vehicleType = await work.VehicleTypes.GetAsync(fareModel.VehicleTypeId,
                        cancellationToken: cancellationToken);
                    if (vehicleType is null)
                    {
                        throw new ApplicationException("Thông tin phương tiện di chuyển không hợp lệ!!");
                    }

                    Setting? settingNightTrip = null;
                    switch (vehicleType.Type)
                    {
                        case VehicleSubType.VI_RIDE:
                            settingNightTrip = await work.Settings.GetAsync(
                                s => s.Key.Equals(SettingKeys.NightTripExtraFeeBike),
                                cancellationToken: cancellationToken);
                            break;
                        case VehicleSubType.VI_CAR:
                            settingNightTrip = await work.Settings.GetAsync(
                                s => s.Key.Equals(SettingKeys.NightTripExtraFeeCar),
                                cancellationToken: cancellationToken);
                            break;

                    }
                    if (settingNightTrip != null)
                    {
                        responseModel.AdditionalFare += double.Parse(settingNightTrip.Value);

                        //responseModel.OriginalFare += responseModel.AdditionalFare;
                        //responseModel.FinalFare += responseModel.AdditionalFare;
                    }

                }
            }

            responseModel.OriginalFare = FareUtilities.RoundToThousands(responseModel.OriginalFare);
            responseModel.FinalFare = responseModel.OriginalFare + responseModel.AdditionalFare;
            //responseModel.FinalFare += responseModel.AdditionalFare;

            // Discount on Total Number of Tickets
            Setting? settingTotalTicket = null;
            if (fareModel.TotalNumberOfTickets >= 50)
            {
                settingTotalTicket = await work.Settings.GetAsync(
                            s => s.Key.Equals(SettingKeys.TicketsDiscount_50),
                            cancellationToken: cancellationToken);
            } else if (fareModel.TotalNumberOfTickets >= 25)
            {
                settingTotalTicket = await work.Settings.GetAsync(
                            s => s.Key.Equals(SettingKeys.TicketsDiscount_25),
                            cancellationToken: cancellationToken);
            } else if (fareModel.TotalNumberOfTickets >= 10)
            {
                settingTotalTicket = await work.Settings.GetAsync(
                            s => s.Key.Equals(SettingKeys.TicketsDiscount_10),
                            cancellationToken: cancellationToken);
            }
            if (settingTotalTicket != null)
            {
                double discountPercent = double.Parse(settingTotalTicket.Value);
                responseModel.NumberTicketsDiscount = FareUtilities.RoundToThousands(
                    responseModel.OriginalFare * discountPercent);

                responseModel.FinalFare -= responseModel.NumberTicketsDiscount;
            }

            //double finalFareEachTrip = FareUtilities.RoundToThousands(responseModel.FinalFare / fareModel.TotalNumberOfTickets);

            responseModel.FinalFare = FareUtilities.RoundToThousands(responseModel.FinalFare);

            return responseModel;
        }

        #region Private Members
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
        
        //public async Task<double> TestCalculateTripFare(double distance)
        //{
        //    Guid vehicleTypeGuid = new Guid("2788F072-56CD-4FA6-A51A-79E6F473BF9F");

        //    Fare fare = await work.Fares.GetAsync(f => f.VehicleTypeId.Equals(vehicleTypeGuid));
        //    IEnumerable<FarePolicy> farePolicies = await work.FarePolicies
        //        .GetAllAsync(query => query.Where(
        //            fp => fp.FareId.Equals(fare.Id)));

        //    FareForCalculationModel fareModel = new FareForCalculationModel(fare, farePolicies.ToList());

        //    double tripFare = FareUtilities.CalculateTripFare(distance, new TimeOnly(15, 00), new TimeOnly(15, 15), fareModel);
        //    return tripFare;
        //}
    }
}
