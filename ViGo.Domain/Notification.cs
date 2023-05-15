using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Notification
    {
        public override Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public NotificationType Type { get; set; }
        public Guid? UserId { get; set; }
        public Guid? EventId { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        public virtual User CreatedByNavigation { get; set; } = null!;
        public virtual Event? Event { get; set; }
        public virtual User UpdatedByNavigation { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}
