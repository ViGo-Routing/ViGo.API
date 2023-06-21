using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Notifications
{
    public class NotificationCreateModel
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public NotificationType Type { get; set; }
        public Guid? UserId { get; set; }
        public Guid? EventId { get; set; }
        public bool IsSentToUser { get; set; } = true;
    }
}
