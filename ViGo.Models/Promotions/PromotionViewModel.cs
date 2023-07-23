using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Events;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;
using ViGo.Models.VehicleTypes;

namespace ViGo.Models.Promotions
{
    public class PromotionViewModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public Guid? EventId { get; set; }
        public EventViewModel? Event { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double DiscountAmount { get; set; }
        public bool? IsPercentage { get; set; }
        public double? MaxDecrease { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? ExpireTime { get; set; }
        public int TotalUsage { get; set; }
        public int? MaxTotalUsage { get; set; }
        public int? UsagePerUser { get; set; }
        public double? MinTotalPrice { get; set; }
        public Guid VehicleTypeId { get; set; }
        public VehicleTypeViewModel VehicleType { get; set; }
        public PromotionStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public PromotionViewModel(Promotion promotion)
        {
            Id = promotion.Id;
            Code = promotion.Code;
            EventId = promotion.EventId;
            Name = promotion.Name;
            Description = promotion.Description;
            DiscountAmount = promotion.DiscountAmount;
            IsPercentage = promotion.IsPercentage;
            MaxDecrease = promotion.MaxDecrease;
            StartTime = promotion.StartTime;
            ExpireTime = promotion.ExpireTime;
            MaxTotalUsage = promotion.MaxTotalUsage;
            TotalUsage = promotion.TotalUsage;
            UsagePerUser = promotion.UsagePerUser;
            MinTotalPrice = promotion.MinTotalPrice;
            VehicleTypeId = promotion.VehicleTypeId;
            Status = promotion.Status;
            CreatedTime = promotion.CreatedTime;
            CreatedBy = promotion.CreatedBy;
            UpdatedTime = promotion.UpdatedTime;
            UpdatedBy = promotion.UpdatedBy;
            IsDeleted = promotion.IsDeleted;
        }

        public PromotionViewModel(Promotion promotion,
            VehicleType vehicleType,
            Event? promotionEvent = null)
        : this(promotion)
        {
            VehicleType = new VehicleTypeViewModel(vehicleType);
            if (promotionEvent != null)
            {
                Event = new EventViewModel(promotionEvent);
            }
        }
    }

    public class PromotionSortingParameters : SortingParameters
    {
        public PromotionSortingParameters()
        {
            OrderBy = QueryStringUtilities.ToSortingCriteria(
                new SortingCriteria(nameof(Promotion.StartTime)));
        }
    }
}
