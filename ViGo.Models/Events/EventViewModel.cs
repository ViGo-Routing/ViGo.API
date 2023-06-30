using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Events
{
    public class EventViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public EventStatus Status { get; set; }

        public EventViewModel(Event ev)
        {
            Id = ev.Id;
            Title = ev.Title;
            Content = ev.Content;
            Status = ev.Status;
        }
    }
}
