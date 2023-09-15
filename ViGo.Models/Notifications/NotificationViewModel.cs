using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Sorting;
using ViGo.Models.Users;

namespace ViGo.Models.Notifications
{
    public class NotificationViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public NotificationType Type { get; set; }
        public Guid? UserId { get; set; }
        //public Guid? EventId { get; set; }
        public NotificationStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public Guid UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }

        //public EventViewModel? Event { get; set; }
        public UserViewModel? User { get; set; }

        public NotificationViewModel(Notification notification)
        {
            Id = notification.Id;
            Title = notification.Title;
            Description = notification.Description;
            Type = notification.Type;
            UserId = notification.UserId;
            //EventId = notification.EventId;
            Status = notification.Status;
            CreatedTime = notification.CreatedTime;
            CreatedBy = notification.CreatedBy;
        }

        public NotificationViewModel(Notification notification,
            UserViewModel? user)
            : this(notification)
        {
            User = user;
        }
    }

    public class NotificationSortingParameters : SortingParameters
    {
        public NotificationSortingParameters()
        {
            OrderBy = QueryStringUtilities.ToSortingCriteria(
                new SortingCriteria(nameof(Notification.CreatedTime)));
        }

    }
}
