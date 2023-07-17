using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Events
{
    public class EventUpdateModel
    {
        public string? Title { get; set; }
        public string? Content { get; set; } 
        public EventStatus? Status { get; set; }
    }
}
