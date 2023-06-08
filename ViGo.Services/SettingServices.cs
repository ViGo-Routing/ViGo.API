using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.Fares;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class SettingServices : BaseServices<Setting>
    {
        private const string TICKETS_DISCOUNT_10 = "10TicketsDiscount";
        private const string TICKETS_DISCOUNT_25 = "25TicketsDiscount";
        private const string TICKETS_DISCOUNT_50 = "50TicketsDiscount";
        public SettingServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<IEnumerable<FareDiscount>> GetFareDiscountsAsync()
        {
            IEnumerable<string> ticketDiscountKeys = new List<string>
            {
                TICKETS_DISCOUNT_10,
                TICKETS_DISCOUNT_25,
                TICKETS_DISCOUNT_50
            };

            IEnumerable<Setting> settings = await work.Settings
                .GetAllAsync(query => query.Where(
                    s => ticketDiscountKeys.Contains(s.Key)));

            IEnumerable<FareDiscount> fareDiscounts = new List<FareDiscount>
            {
                new FareDiscount(10, double.Parse(settings
                .SingleOrDefault(s => s.Key.Equals(TICKETS_DISCOUNT_10)).Value)),
                new FareDiscount(25, double.Parse(settings
                .SingleOrDefault(s => s.Key.Equals(TICKETS_DISCOUNT_25)).Value)),
                new FareDiscount(50, double.Parse(settings
                .SingleOrDefault(s => s.Key.Equals(TICKETS_DISCOUNT_50)).Value))
            };

            return fareDiscounts;
        } 
    }
}
