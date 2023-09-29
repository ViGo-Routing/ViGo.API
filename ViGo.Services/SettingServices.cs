using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Fares;
using ViGo.Models.Settings;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class SettingServices : BaseServices
    {
        //private const string TICKETS_DISCOUNT_10 = "10TicketsDiscount";
        //private const string TICKETS_DISCOUNT_25 = "25TicketsDiscount";
        //private const string TICKETS_DISCOUNT_50 = "50TicketsDiscount";

        private IDictionary<string, string> settingKeys = new Dictionary<string, string>
        {
            //{ SettingKeys.NightTripExtraFeeCar_Key, SettingKeys.NightTripExtraFeeCar_Description },
            { SettingKeys.NightTripExtraFeeBike_Key, SettingKeys.NightTripExtraFeeBike_Description },
            //{ SettingKeys.TicketsDiscount_10_Key, SettingKeys.TicketsDiscount_10_Description },
            //{ SettingKeys.TicketsDiscount_25_Key, SettingKeys.TicketsDiscount_25_Description },
            //{ SettingKeys.TicketsDiscount_50_Key, SettingKeys.TicketsDiscount_50_Description },
            { SettingKeys.WeeklyTicketsDiscount_2_Key, SettingKeys.WeeklyTicketsDiscount_2_Description },
            { SettingKeys.WeeklyTicketsDiscount_5_Key, SettingKeys.WeeklyTicketsDiscount_5_Description },
            { SettingKeys.MonthlyTicketsDiscount_2_Key, SettingKeys.MonthlyTicketsDiscount_2_Description },
            { SettingKeys.MonthlyTicketsDiscount_4_Key, SettingKeys.MonthlyTicketsDiscount_4_Description },
            { SettingKeys.MonthlyTicketsDiscount_6_Key, SettingKeys.MonthlyTicketsDiscount_6_Description },
            //{ SettingKeys.QuarterlyTicketsDiscount_2_Key, SettingKeys.QuarterlyTicketsDiscount_2_Description },
            { SettingKeys.DriverWagePercent_Key, SettingKeys.DriverWagePercent_Description },
            { SettingKeys.TripMustStartBefore_Key, SettingKeys.TripMustStartBefore_Description },
            { SettingKeys.TripMustBeBookedBefore_Key, SettingKeys.TripMustBeBookedBefore_Description }
        };

        public SettingServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        //public async Task<IEnumerable<FareDiscount>> GetFareDiscountsAsync(
        //    CancellationToken cancellationToken)
        //{
        //    IEnumerable<string> ticketDiscountKeys = new List<string>
        //    {
        //        SettingKeys.TicketsDiscount_10_Key,
        //        SettingKeys.TicketsDiscount_25_Key,
        //        SettingKeys.TicketsDiscount_50_Key
        //    };

        //    IEnumerable<Setting> settings = await work.Settings
        //        .GetAllAsync(query => query.Where(
        //            s => ticketDiscountKeys.Contains(s.Key)),
        //            cancellationToken: cancellationToken);

        //    IEnumerable<FareDiscount> fareDiscounts = new List<FareDiscount>
        //    {
        //        new FareDiscount(10, double.Parse(settings
        //        .SingleOrDefault(s => s.Key.Equals(SettingKeys.TicketsDiscount_10_Key)).Value)),
        //        new FareDiscount(25, double.Parse(settings
        //        .SingleOrDefault(s => s.Key.Equals(SettingKeys.TicketsDiscount_25_Key)).Value)),
        //        new FareDiscount(50, double.Parse(settings
        //        .SingleOrDefault(s => s.Key.Equals(SettingKeys.TicketsDiscount_50_Key)).Value))
        //    };

        //    return fareDiscounts;
        //}

        public async Task<IEnumerable<SettingViewModel>> GetSettingsAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Setting> settings = await work.Settings.GetAllAsync(
                cancellationToken: cancellationToken);

            IList<SettingViewModel> models = new List<SettingViewModel>();
            foreach (Setting setting in settings)
            {
                if (settingKeys.TryGetValue(setting.Key, out string? description)
                    && description != null)
                {
                    SettingViewModel model = new SettingViewModel(setting, description);
                    models.Add(model);
                }
            }

            models = models.OrderBy(s => s.Description).ToList();
            return models;
        }

        public async Task<Setting> UpdateSettingAsync(
            SettingUpdateModel updateModel, CancellationToken cancellationToken)
        {
            Setting? setting = await work.Settings
                .GetAsync(s => s.Key.Equals(updateModel.Key),
                cancellationToken: cancellationToken);

            if (setting is null)
            {
                throw new ApplicationException("Cấu hình không tồn tại!!!");
            }

            //setting.DataType = updateModel.DataType;
            setting.Value = updateModel.Value;
            //setting.DataUnit = updateModel.DataUnit;
            //setting.Type = updateModel.Type;

            await work.Settings.UpdateAsync(setting);
            await work.SaveChangesAsync(cancellationToken);


            return setting;
        }
    }
}
