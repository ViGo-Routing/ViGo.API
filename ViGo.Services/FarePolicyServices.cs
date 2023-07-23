using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.FarePolicies;
using ViGo.Repository.Core;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class FarePolicyServices : BaseServices
    {
        public FarePolicyServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<FarePolicy> UpdateFarePolicyAsync(
            FarePolicyUpdateModel model, CancellationToken cancellationToken)
        {
            FarePolicy? farePolicy = await work.FarePolicies
                .GetAsync(model.Id, cancellationToken: cancellationToken);
            if (farePolicy is null)
            {
                throw new ApplicationException("Chính sách giá không tồn tại!!");
            }

            if (model.PricePerKm <= 1000)
            {
                throw new ApplicationException("Giá tiền mỗi km phải lớn hơn 1.000 VND!");
            }

            farePolicy.PricePerKm = model.PricePerKm;
            await work.FarePolicies.UpdateAsync(farePolicy);
            await work.SaveChangesAsync(cancellationToken);

            return farePolicy;
        }

        public async Task<IEnumerable<FarePolicyViewModel>> GetFarePoliciesAsync(
            Guid fareId, CancellationToken cancellationToken)
        {
            IEnumerable<FarePolicy> farePolicies = await work.FarePolicies
                .GetAllAsync(query => query.Where(p => p.FareId.Equals(fareId)),
                 cancellationToken: cancellationToken);

            farePolicies = farePolicies.OrderBy(p => p.MinDistance);

            IEnumerable<FarePolicyViewModel> models = from policy in farePolicies
                                                      select new FarePolicyViewModel(policy);
            return models;
        }
    }
}
