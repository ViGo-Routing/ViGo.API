using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Models.Events;
using ViGo.Models.VehicleTypes;

namespace ViGo.Models.Promotions
{
    public class PromotionUpdateModel
    {
        public Guid Id { get; set; }
        //public string? Code { get; set; } // Cannot update Code
        public Guid? EventId { get; set; }
        public string? Name { get; set; } 
        public string? Description { get; set; }
        public double? DiscountAmount { get; set; }
        public bool? IsPercentage { get; set; }
        public double? MaxDecrease { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public int? MaxTotalUsage { get; set; }
        public int? UsagePerUser { get; set; }
        public double? MinTotalPrice { get; set; }
        public Guid? VehicleTypeId { get; set; }
        public PromotionStatus? Status { get; set; } = PromotionStatus.AVAILABLE;
    }
}
