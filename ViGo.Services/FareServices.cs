using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.FarePolicies;
using ViGo.Models.Fares;
using ViGo.Models.VehicleTypes;
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
                s => s.Key.Equals(SettingKeys.DriverWagePercent_Key), cancellationToken: cancellationToken);

            if (driverWageSetting is null)
            {
                throw new ApplicationException("Chiết khấu chuyến đi dành cho tài xế chưa được thiết lập!!");
            }

            double percent = double.Parse(driverWageSetting.Value);
            return FareUtilities.RoundToThousands(bookingDetailFare * percent);
        }

        public async Task<double> CalculateDriverPickFee(double bookingDetailFare,
            CancellationToken cancellationToken)
        {
            Setting? driverWageSetting = await work.Settings.GetAsync(
                s => s.Key.Equals(SettingKeys.DriverWagePercent_Key), cancellationToken: cancellationToken);

            if (driverWageSetting is null)
            {
                throw new ApplicationException("Chiết khấu chuyến đi dành cho tài xế chưa được thiết lập!!");
            }

            double percent = double.Parse(driverWageSetting.Value);
            return FareUtilities.RoundToThousands(bookingDetailFare * (1 - percent));
        }

        public async Task<FareCalculateResponseModel> CalculateFareBasedOnDistance(
            FareCalculateRequestModel fareModel, CancellationToken cancellationToken)
        {
            if (fareModel.TripType == BookingType.ROUND_TRIP)
            {
                if (fareModel.RoundTripBeginTime is null)
                {
                    throw new ApplicationException("Thời gian cho chuyến đi khứ hồi chưa được thiết lập!!!");

                }

                if (fareModel.TotalNumberOfTickets % 2 != 0)
                {
                    throw new ApplicationException("Tổng số chuyến đi không phù hợp cho hành trình khứ hồi!!");
                }
            }

            Fare? fare = await work.Fares.GetAsync(
                f => f.VehicleTypeId.Equals(fareModel.VehicleTypeId),
                cancellationToken: cancellationToken);
            if (fare is null)
            {
                throw new ApplicationException("Loại phương tiện chưa được cấu hình chính sách tính giá!");
            }
            IEnumerable<FarePolicy> farePolicies = await work.FarePolicies
                .GetAllAsync(query => query.Where(
                    fp => fp.FareId.Equals(fare.Id)), cancellationToken: cancellationToken);

            FareForCalculationModel fareCalculationModel = new FareForCalculationModel(fare, farePolicies.ToList());

            FareCalculateResponseModel responseModel = new FareCalculateResponseModel
            {
                AdditionalFare = 0,
                FinalFare = 0,
                NumberTicketsDiscount = 0,
                OriginalFare = 0,
                RoutineTypeDiscount = 0,
                RoundTripOriginalFare = 0,
                RoundTripAdditionalFare = 0,
                RoundTripFinalFare = 0
            };

            if (fareModel.Distance <= fareCalculationModel.MinimumBaseDistance)
            {
                responseModel.OriginalFare = fareCalculationModel.MinimumBasePrice;
                //responseModel.RoundTripOriginalFare = fareCalculationModel.MinimumBasePrice;
                //responseModel.FinalFare = fareCalculationModel.MinimumBasePrice;
            }
            else
            {
                // distance > fare.MinimumBaseDistance
                responseModel.OriginalFare = fareCalculationModel.MinimumBasePrice;
                //responseModel.RoundTripOriginalFare = fareCalculationModel.MinimumBasePrice;
                //responseModel.FinalFare = fareCalculationModel.MinimumBasePrice;

                int policyCount = fareCalculationModel.FarePolicies.Count;
                double subDistance = fareModel.Distance - fareCalculationModel.MinimumBaseDistance;

                int policyDistanceIndex = PolicyDistanceFindIndex(fareModel.Distance, 
                    fareCalculationModel.MinimumBaseDistance, fareCalculationModel.FarePolicies);
                if (policyDistanceIndex < 0)
                {
                    throw new ApplicationException("Thông tin khoảng cách và giá tiền không hợp lệ!");
                }
                else if (policyDistanceIndex == 0)
                {
                    responseModel.OriginalFare += subDistance * fareCalculationModel.FarePolicies[policyDistanceIndex].PricePerKm.Value;
                    //responseModel.RoundTripOriginalFare += subDistance * fareCalculationModel.FarePolicies[policyDistanceIndex].PricePerKm.Value;
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

            //responseModel.OriginalFare *= fareModel.TotalNumberOfTickets;

            if (fareModel.TripType == BookingType.ROUND_TRIP)
            {
                //responseModel.OriginalFare *= 2;
                responseModel.RoundTripOriginalFare = responseModel.OriginalFare;
                responseModel.RoundTripFinalFare = responseModel.FinalFare;
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
                            s => s.Key.Equals(SettingKeys.NightTripExtraFeeBike_Key),
                            cancellationToken: cancellationToken);
                        break;
                    //case VehicleSubType.VI_CAR:
                    //    settingNightTrip = await work.Settings.GetAsync(
                    //        s => s.Key.Equals(SettingKeys.NightTripExtraFeeCar_Key),
                    //        cancellationToken: cancellationToken);
                    //    break;

                }
                if (settingNightTrip != null)
                {
                    responseModel.AdditionalFare += double.Parse(settingNightTrip.Value);

                    //responseModel.OriginalFare += responseModel.AdditionalFare;
                    //responseModel.FinalFare += responseModel.AdditionalFare;
                }

            }

            // Round Trip
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
                                s => s.Key.Equals(SettingKeys.NightTripExtraFeeBike_Key),
                                cancellationToken: cancellationToken);
                            break;
                        //case VehicleSubType.VI_CAR:
                        //    settingNightTrip = await work.Settings.GetAsync(
                        //        s => s.Key.Equals(SettingKeys.NightTripExtraFeeCar_Key),
                        //        cancellationToken: cancellationToken);
                        //    break;

                    }
                    if (settingNightTrip != null)
                    {
                        responseModel.RoundTripAdditionalFare += double.Parse(settingNightTrip.Value);

                        //responseModel.OriginalFare += responseModel.AdditionalFare;
                        //responseModel.FinalFare += responseModel.AdditionalFare;
                    }

                }
            }

            //responseModel.OriginalFare = FareUtilities.RoundToThousands(responseModel.OriginalFare);
            //responseModel.FinalFare = responseModel.OriginalFare + responseModel.AdditionalFare;
            //responseModel.FinalFare += responseModel.AdditionalFare;

            // Discount on Total Number of Tickets
            //Setting? settingTotalTicket = null;
            //if (fareModel.TotalNumberOfTickets >= 50)
            //{
            //    settingTotalTicket = await work.Settings.GetAsync(
            //                s => s.Key.Equals(SettingKeys.TicketsDiscount_50_Key),
            //                cancellationToken: cancellationToken);
            //}
            //else if (fareModel.TotalNumberOfTickets >= 25)
            //{
            //    settingTotalTicket = await work.Settings.GetAsync(
            //                s => s.Key.Equals(SettingKeys.TicketsDiscount_25_Key),
            //                cancellationToken: cancellationToken);
            //}
            //else if (fareModel.TotalNumberOfTickets >= 10)
            //{
            //    settingTotalTicket = await work.Settings.GetAsync(
            //                s => s.Key.Equals(SettingKeys.TicketsDiscount_10_Key),
            //                cancellationToken: cancellationToken);
            //}
            //if (settingTotalTicket != null)
            //{
            //    double discountPercent = double.Parse(settingTotalTicket.Value);
            //    responseModel.NumberTicketsDiscount = FareUtilities.RoundToThousands(
            //        responseModel.OriginalFare * discountPercent);

            //    //responseModel.FinalFare -= responseModel.NumberTicketsDiscount;

            //    if (fareModel.TripType == BookingType.ROUND_TRIP)
            //    {
            //        responseModel.RoundTripNumberTicketsDiscount = FareUtilities.RoundToThousands(
            //            responseModel.RoundTripOriginalFare * discountPercent);
            //    }
            //}
            // Discount on routine type
            //if (fareModel.)
            Setting? discountRoutineSetting = null;
            if (fareModel.RoutineType == RoutineType.WEEKLY)
            {
                if (fareModel.EachWeekTripsCount >= 4 
                    && fareModel.TotalNumberOfTickets > fareModel.EachWeekTripsCount)
                {
                    if (fareModel.TotalFrequencyCount >= 2)
                    {
                        discountRoutineSetting = await work.Settings
                            .GetAsync(s => s.Key.Equals(SettingKeys.WeeklyTicketsDiscount_2_Key),
                            cancellationToken: cancellationToken);
                    }
                    else if (fareModel.TotalFrequencyCount >= 5)
                    {
                        discountRoutineSetting = await work.Settings
                            .GetAsync(s => s.Key.Equals(SettingKeys.WeeklyTicketsDiscount_5_Key),
                            cancellationToken: cancellationToken);
                    }
                }
                
            } else if (fareModel.RoutineType == RoutineType.MONTHLY)
            {
                if (fareModel.EachWeekTripsCount >= 4
                    && fareModel.TotalNumberOfTickets > fareModel.EachWeekTripsCount)
                {
                    if (fareModel.TotalFrequencyCount >= 2)
                    {
                        discountRoutineSetting = await work.Settings
                            .GetAsync(s => s.Key.Equals(SettingKeys.MonthlyTicketsDiscount_2_Key),
                            cancellationToken: cancellationToken);
                    }
                    else if (fareModel.TotalFrequencyCount >= 4)
                    {
                        discountRoutineSetting = await work.Settings
                            .GetAsync(s => s.Key.Equals(SettingKeys.MonthlyTicketsDiscount_4_Key),
                            cancellationToken: cancellationToken);
                    }
                    else if (fareModel.TotalFrequencyCount >= 6)
                    {
                        discountRoutineSetting = await work.Settings
                            .GetAsync(s => s.Key.Equals(SettingKeys.MonthlyTicketsDiscount_6_Key),
                            cancellationToken: cancellationToken);
                    }
                }
            }
            if (discountRoutineSetting != null)
            {
                double discountPercent = double.Parse(discountRoutineSetting.Value);
                responseModel.RoutineTypeDiscount = 
                    responseModel.OriginalFare * discountPercent;

                //responseModel.FinalFare -= responseModel.NumberTicketsDiscount;

                if (fareModel.TripType == BookingType.ROUND_TRIP)
                {
                    responseModel.RoundTripRoutineTypeDiscount = 
                        responseModel.RoundTripOriginalFare * discountPercent;
                }
            }

            //double finalFareEachTrip = FareUtilities.RoundToThousands(responseModel.FinalFare / fareModel.TotalNumberOfTickets);
            int totalTickets = 0;
            if (fareModel.TripType == BookingType.ONE_WAY)
            {
                totalTickets = fareModel.TotalNumberOfTickets;
            }
            else
            {
                totalTickets = fareModel.TotalNumberOfTickets / 2;
            }

            responseModel.OriginalFare = FareUtilities.RoundToThousands(responseModel.OriginalFare  * totalTickets);
            responseModel.NumberTicketsDiscount = FareUtilities.RoundToThousands(responseModel.NumberTicketsDiscount * totalTickets);
            responseModel.RoutineTypeDiscount = FareUtilities.RoundToThousands(responseModel.RoutineTypeDiscount * totalTickets);
            responseModel.AdditionalFare = FareUtilities.RoundToThousands(responseModel.AdditionalFare * totalTickets);
            responseModel.FinalFare = responseModel.OriginalFare + responseModel.AdditionalFare 
                - responseModel.NumberTicketsDiscount
                - responseModel.RoutineTypeDiscount;

            if (fareModel.TripType == BookingType.ROUND_TRIP)
            {
                responseModel.RoundTripOriginalFare = FareUtilities.RoundToThousands(
                    responseModel.RoundTripOriginalFare * totalTickets);
                responseModel.RoundTripNumberTicketsDiscount = FareUtilities.RoundToThousands(responseModel.RoundTripNumberTicketsDiscount * totalTickets);
                responseModel.RoundTripRoutineTypeDiscount = FareUtilities.RoundToThousands(responseModel.RoundTripRoutineTypeDiscount * totalTickets);
                responseModel.RoundTripAdditionalFare = FareUtilities.RoundToThousands(responseModel.RoundTripAdditionalFare * totalTickets);
                responseModel.RoundTripFinalFare = responseModel.RoundTripOriginalFare
                    + responseModel.RoundTripAdditionalFare 
                    - responseModel.RoundTripNumberTicketsDiscount
                    - responseModel.RoundTripRoutineTypeDiscount;
            }

            //responseModel.FinalFare = FareUtilities.RoundToThousands(responseModel.FinalFare);

            return responseModel;
        }

        public async Task<FareViewModel> CreateFareAsync(FareCreateModel model,
            CancellationToken cancellationToken)
        {
            if (model.FarePolicies is null || !model.FarePolicies.Any())
            {
                throw new ApplicationException("Chính sách giá không có giá trị nào!!");
            }

            VehicleType? vehicleType = await work.VehicleTypes.GetAsync(model.VehicleTypeId,
                cancellationToken: cancellationToken);
            if (vehicleType is null)
            {
                throw new ApplicationException("Loại phương tiện không tồn tại!!!");
            }

            Fare? checkFare = await work.Fares.GetAsync(f => f.VehicleTypeId.Equals(model.VehicleTypeId),
                cancellationToken: cancellationToken);
            if (checkFare != null)
            {
                throw new ApplicationException("Loại phương tiện này đã được thiết lập giá!");
            }

            if (model.BasePrice <= 0)
            {
                throw new ApplicationException("Giá tiền cơ bản phải lớn hơn 0 (đơn vị VND)!");
            }
            if (model.BaseDistance <= 0)
            {
                throw new ApplicationException("Khoảng cách cơ bản phải lớn 0 (đơn vị km)!");
            }

            if (model.FarePolicies.Count == 0)
            {
                throw new ApplicationException("Chính sách giá chưa được thiết lập!!");
            }

            Fare fare = new Fare
            {
                VehicleTypeId = model.VehicleTypeId,
                BasePrice = model.BasePrice,
                BaseDistance = model.BaseDistance
            };

            await work.Fares.InsertAsync(fare, cancellationToken: cancellationToken);

            IList<FarePolicy> farePolicies = new List<FarePolicy>();

            foreach (FarePolicyListItemModel policy in model.FarePolicies)
            {
                IsValidPolicy(policy);
                farePolicies.Add(new FarePolicy
                {
                    FareId = fare.Id,
                    MinDistance = policy.MinDistance,
                    MaxDistance = policy.MaxDistance,
                    PricePerKm = policy.PricePerKm
                });
            }
            IsValidPolicies(model.FarePolicies, fare,
                cancellationToken: cancellationToken);

            await work.FarePolicies.InsertAsync(farePolicies,
                cancellationToken: cancellationToken);

            await work.SaveChangesAsync(cancellationToken);

            IEnumerable<FarePolicyViewModel> farePolicyViewModels =
                from policy in farePolicies
                select new FarePolicyViewModel(policy);
            FareViewModel fareViewModel = new FareViewModel(fare,
                new VehicleTypeViewModel(vehicleType),
                farePolicyViewModels.ToList());
            return fareViewModel;
        }

        public async Task<IEnumerable<FareViewModel>> GetFaresAsync(
            //PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Fare> fares = await work.Fares.GetAllAsync(cancellationToken: cancellationToken);

            int total = fares.Count();

            //fares = fares.ToPagedEnumerable(pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<Guid> fareIds = fares.Select(f => f.Id);
            IEnumerable<Guid> vehicleTypeIds = fares.Select(f => f.VehicleTypeId).Distinct();

            IEnumerable<VehicleType> vehicleTypes = await work.VehicleTypes.GetAllAsync(
                query => query.Where(v => vehicleTypeIds.Contains(v.Id)), cancellationToken: cancellationToken);
            //IEnumerable<FarePolicy> farePolicies = await work.FarePolicies.GetAllAsync(
            //    query => query.Where(p => fareIds.Contains(p.FareId)), cancellationToken: cancellationToken);

            IList<FareViewModel> fareModels = new List<FareViewModel>();
            foreach (Fare fare in fares)
            {
                VehicleType vehicleType = vehicleTypes.SingleOrDefault(v => v.Id.Equals(fare.VehicleTypeId));
                //IEnumerable<FarePolicy> policies = farePolicies.Where(p => p.FareId.Equals(fare.Id));
                //IEnumerable<FarePolicyViewModel> policyViewModels = from policy in policies
                //                                                    select new FarePolicyViewModel(policy);
                fareModels.Add(new FareViewModel(fare, new VehicleTypeViewModel(vehicleType)/*, policyViewModels*/));
            }

            //return fareModels.ToPagedEnumerable(pagination.PageNumber,
            //    pagination.PageSize, total, context);
            return fareModels;
        }

        public async Task<FareViewModel> GetFareAsync(Guid fareId, CancellationToken cancellationToken)
        {
            Fare? fare = await work.Fares.GetAsync(fareId, cancellationToken: cancellationToken);
            if (fare is null)
            {
                throw new ApplicationException("Cấu hình giá không tồn tại!");
            }

            VehicleType vehicleType = await work.VehicleTypes.GetAsync(fare.VehicleTypeId,
                cancellationToken: cancellationToken);

            IEnumerable<FarePolicy> farePolicies = await work.FarePolicies.GetAllAsync(
                query => query.Where(p => p.FareId.Equals(fare.Id)), cancellationToken: cancellationToken);
            IEnumerable<FarePolicyViewModel> policyViewModels = from policy in farePolicies
                                                                select new FarePolicyViewModel(policy);

            return new FareViewModel(fare, new VehicleTypeViewModel(vehicleType), policyViewModels);
        }

        public async Task<FareViewModel> GetVehicleTypeFareAsync(Guid vehicleTypeId, CancellationToken cancellationToken)
        {
            Fare? fare = await work.Fares.GetAsync(f => f.VehicleTypeId.Equals(vehicleTypeId),
                cancellationToken: cancellationToken);
            if (fare is null)
            {
                throw new ApplicationException("Loại phương tiện chưa được thiết lập giá!");
            }

            VehicleType vehicleType = await work.VehicleTypes.GetAsync(fare.VehicleTypeId,
                cancellationToken: cancellationToken);

            IEnumerable<FarePolicy> farePolicies = await work.FarePolicies.GetAllAsync(
                query => query.Where(p => p.FareId.Equals(fare.Id)), cancellationToken: cancellationToken);
            IEnumerable<FarePolicyViewModel> policyViewModels = from policy in farePolicies
                                                                select new FarePolicyViewModel(policy);

            return new FareViewModel(fare, new VehicleTypeViewModel(vehicleType), policyViewModels);
        }

        public async Task<FareViewModel> UpdateFareAsync(FareUpdateModel model,
            CancellationToken cancellationToken)
        {
            Fare? fare = await work.Fares.GetAsync(model.Id,
                cancellationToken: cancellationToken);
            if (fare is null)
            {
                throw new ApplicationException("Cấu hình giá không tồn tại!");
            }

            IEnumerable<FarePolicy> currentPolicies = await work.FarePolicies
                    .GetAllAsync(query => query.Where(p => p.FareId.Equals(fare.Id)),
                    cancellationToken: cancellationToken);

            if (model.BasePrice.HasValue &&
                model.BasePrice.Value != fare.BasePrice)
            {
                if (model.BasePrice <= 0)
                {
                    throw new ApplicationException("Giá tiền cơ bản phải lớn hơn 0 (đơn vị VND)!");
                }

                fare.BasePrice = model.BasePrice.Value;
            }

            if (model.BaseDistance.HasValue &&
                model.BaseDistance.Value != fare.BaseDistance)
            {
                if (model.FarePolicies is null || model.FarePolicies.Count == 0)
                {
                    throw new ApplicationException("Cập nhật khoảng cách cơ bản yêu cầu phải kèm theo " +
                        "danh sách các chính sách giá phù hợp!");
                }

                if (model.BaseDistance <= 0)
                {
                    throw new ApplicationException("Khoảng cách cơ bản phải lớn 0 (đơn vị km)!");
                }

                fare.BaseDistance = model.BaseDistance.Value;
            }

            if (model.FarePolicies != null && model.FarePolicies.Count > 0)
            {
                IList<FarePolicy> farePolicies = new List<FarePolicy>();

                foreach (FarePolicyListItemModel policy in model.FarePolicies)
                {
                    IsValidPolicy(policy);
                    farePolicies.Add(new FarePolicy
                    {
                        FareId = fare.Id,
                        MinDistance = policy.MinDistance,
                        MaxDistance = policy.MaxDistance,
                        PricePerKm = policy.PricePerKm
                    });
                }
                IsValidPolicies(model.FarePolicies, fare,
                    cancellationToken: cancellationToken);

                // Delete current policies to insert the new ones
                foreach (FarePolicy currentPolicy in currentPolicies)
                {
                    await work.FarePolicies.DeleteAsync(currentPolicy,
                        isSoftDelete: false,
                        cancellationToken: cancellationToken);
                }

                // Insert the new ones
                await work.FarePolicies.InsertAsync(farePolicies,
                    cancellationToken: cancellationToken);

                currentPolicies = farePolicies;
            }

            // Update fare
            await work.Fares.UpdateAsync(fare);

            await work.SaveChangesAsync(cancellationToken);

            IEnumerable<FarePolicyViewModel> farePolicyViewModels =
                from policy in currentPolicies
                select new FarePolicyViewModel(policy);

            VehicleType vehicleType = await work.VehicleTypes.GetAsync(fare.VehicleTypeId,
                cancellationToken: cancellationToken);

            FareViewModel fareViewModel = new FareViewModel(fare,
                new VehicleTypeViewModel(vehicleType),
                farePolicyViewModels.ToList());

            return fareViewModel;
        }

        public async Task<Fare> DeleteFareAsync(Guid fareId, CancellationToken cancellationToken)
        {
            Fare? fare = await work.Fares.GetAsync(fareId,
                cancellationToken: cancellationToken);
            if (fare is null)
            {
                throw new ApplicationException("Cấu hình giá không tồn tại!");
            }

            IEnumerable<FarePolicy> currentPolicies = await work.FarePolicies
                    .GetAllAsync(query => query.Where(p => p.FareId.Equals(fare.Id)),
                    cancellationToken: cancellationToken);

            foreach (FarePolicy policy in currentPolicies)
            {
                await work.FarePolicies.DeleteAsync(policy, cancellationToken: cancellationToken);
            }
            await work.Fares.DeleteAsync(fare, cancellationToken: cancellationToken);

            await work.SaveChangesAsync(cancellationToken);

            return fare;
        }

        #region Private Members
        private int PolicyDistanceFindIndex(double distance,
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

        #region FarePolicy
        private void IsValidPolicy(FarePolicyListItemModel farePolicy)
        {
            if (farePolicy.MinDistance <= 0)
            {
                throw new ApplicationException("Khoảng cách tối thiểu phải lớn hơn 0!");
            }
            if (farePolicy.MaxDistance.HasValue
                && farePolicy.MaxDistance.Value <= 0)
            {
                throw new ApplicationException("Khoảng cách tối đa phải lớn hơn 0!");
            }
            if (farePolicy.PricePerKm <= 1000)
            {
                throw new ApplicationException("Giá tiền mỗi km phải lớn hơn 1.000 VND!");
            }

            if (farePolicy.MaxDistance.HasValue)
            {
                if (farePolicy.MinDistance > farePolicy.MaxDistance.Value)
                {
                    (farePolicy.MinDistance, farePolicy.MaxDistance) =
                        (farePolicy.MaxDistance.Value, farePolicy.MinDistance);
                }
            }

        }

        private void IsValidPolicies(IEnumerable<FarePolicyListItemModel> farePolicies,
            Fare fare, bool isUpdate = true,
            CancellationToken cancellationToken = default)
        {
            if (!farePolicies.Any())
            {
                throw new ApplicationException("Không có chính sách giá nào được thiết lập!!");
            }

            if (fare is null)
            {
                throw new ApplicationException("Thiếu thông tin giá chính!!");
            }

            farePolicies = farePolicies.OrderBy(f => f.MinDistance);
            FarePolicyListItemModel minPolicy = farePolicies.FirstOrDefault();
            if (fare.BaseDistance != minPolicy.MinDistance)
            {
                throw new ApplicationException($"Chính sách giá không phù hợp! " +
                    $"Chính sách giá có khoảng cách tối thiểu nhỏ nhất là {minPolicy.MinDistance} km, trong khi " +
                    $"khoảng cách cơ bản là {fare.BaseDistance} km (hai giá trị này phải giống nhau)");
            }

            IEnumerable<FarePolicyListItemModel> maxPolicies = farePolicies.Where(f => f.MaxDistance is null);
            if (!maxPolicies.Any())
            {
                throw new ApplicationException("Chính sách giá không phù hợp! Phải có một chính sách " +
                    "với khoảng cách tối đa là null (không được cấu hình)!");
            }
            if (maxPolicies.Count() > 1)
            {
                throw new ApplicationException("Chính sách giá không phù hợp! Chỉ được có một chính sách " +
                        "với khoảng cách tối đa là null (không được cấu hình)!");
            }

            IList<FareDistanceRange> fareDistanceRanges = (from policy in farePolicies
                                                           select new FareDistanceRange(policy.MinDistance, policy.MaxDistance))
                                                          .ToList();

            if (fareDistanceRanges[fareDistanceRanges.Count - 1].MaxDistance.HasValue)
            {
                throw new ApplicationException("Chính sách giá không phù hợp! Chính sách có khoảng cách tối thiểu lớn nhất " +
                    "phải có khoảng cách tối đa là null (không được cấu hình)!");
            }
            for (int i = 0; i < fareDistanceRanges.Count - 1; i++)
            {
                FareDistanceRange current = fareDistanceRanges[i];
                FareDistanceRange next = fareDistanceRanges[i + 1];

                //if (!current.MaxDistance.HasValue)
                //{
                //    throw new ApplicationException("Chính sách giá không phù hợp! Chỉ được có một chính sách " +
                //        "với khoảng cách tối đa là null (không được cấu hình)!");
                //}
                //if (!next.MaxDistance.HasValue)
                //{
                //    throw new ApplicationException("Chính sách giá không phù hợp! Chỉ được có một chính sách " +
                //        "với khoảng cách tối đa là null (không được cấu hình)!");
                //}
                if (current.MaxDistance.Value != next.MinDistance)
                {
                    throw new ApplicationException("Chính sách giá không phù hợp!");
                }

                current.IsOverlap(next,
                    $"Hai chính sách giá bị trùng lặp nhau! " +
                    $"Lịch trình 1: " +
                        (current.MaxDistance.HasValue ?
                            $"từ {current.MinDistance} km đến {current.MaxDistance} km."
                            : $"từ {current.MinDistance} km trở lên.") +
                    $"\nLịch trình 2: " +
                        (next.MaxDistance.HasValue ?
                            $"từ {next.MinDistance} km đến {next.MaxDistance} km."
                            : $"từ {next.MinDistance} km trờ lên."));

            }
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
