using Newtonsoft.Json;
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
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public virtual Event? Event { get; set; }
        [JsonIgnore]
        public virtual User? User { get; set; }
    }
}
