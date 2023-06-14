using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.Fares;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;

namespace ViGo.Services
{
    public class FareServices : BaseServices
    {
        public FareServices(IUnitOfWork work) : base(work)
        {
        }

        public async Task<double> TestCalculateTripFare(double distance)
        {
            Guid vehicleTypeGuid = new Guid("2788F072-56CD-4FA6-A51A-79E6F473BF9F");

            Fare fare = await work.Fares.GetAsync(f => f.VehicleTypeId.Equals(vehicleTypeGuid));
            IEnumerable<FarePolicy> farePolicies = await work.FarePolicies
                .GetAllAsync(query => query.Where(
                    fp => fp.FareId.Equals(fare.Id)));

            FareForCalculationModel fareModel = new FareForCalculationModel(fare, farePolicies.ToList());

            double tripFare = FareUtilities.CalculateTripFare(distance, new TimeOnly(15, 00), new TimeOnly(15, 15), fareModel);
            return tripFare;
        }
    }
}
