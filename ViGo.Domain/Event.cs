using System;
using System.Collections.Generic;
using ViGo.Domain.Enumerations;

namespace ViGo.Domain
{
    public partial class Event
    {
        public Event()
        {
            Notifications = new HashSet<Notification>();
        }

        public override Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public EventType Type { get; set; }
        public EventStatus Status { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; }
    }
}
