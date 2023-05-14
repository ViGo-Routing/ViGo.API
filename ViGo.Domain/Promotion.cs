﻿using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Promotion
    {
        public Promotion()
        {
            Bookings = new HashSet<Booking>();
        }

        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public double DiscountAmount { get; set; }
        public bool? IsPercentage { get; set; }
        public double? MaxDecrease { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? ExpireTime { get; set; }
        public int TotalUsage { get; set; }
        public int? UsagePerUser { get; set; }
        public double? MinTotalPrice { get; set; }
        public Guid VehicleTypeId { get; set; }
        public PromotionStatus Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual VehicleType VehicleType { get; set; } = null!;
        public virtual ICollection<Booking> Bookings { get; set; }
    }
}
