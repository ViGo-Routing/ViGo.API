using ViGo.Domain.Enumerations;

namespace ViGo.Models.Notifications
{
    public class NotificationCreateModel
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public NotificationType Type { get; set; } = NotificationType.SPECIFIC_USER;
        public Guid? UserId { get; set; }
        //public Guid? EventId { get; set; }
        public bool IsSentToUser { get; set; } = true;
    }
}
