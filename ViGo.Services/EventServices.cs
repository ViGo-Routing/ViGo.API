using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Events;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;

namespace ViGo.Services
{
    public class EventServices : BaseServices
    {
        public EventServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<EventViewModel>> GetAllEvents(
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Event> events = await work.Events.GetAllAsync(cancellationToken: cancellationToken);
            int totalRecords = events.Count();

            events = events.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<EventViewModel> eventViews = from eventView in events
                                                     orderby eventView.Status descending
                                                     select new EventViewModel(eventView);

            return eventViews.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<IPagedEnumerable<EventViewModel>> GetAllEventsActive(
            PaginationParameter pagination,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Event> events = await work.Events.GetAllAsync(
                q => q.Where(x => x.Status.Equals(EventStatus.ACTIVE)), cancellationToken: cancellationToken);
            int totalRecords = events.Count();

            events = events.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<EventViewModel> eventViews = from eventView in events
                                                     select new EventViewModel(eventView);

            return eventViews.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<EventViewModel> GetEventByID(Guid id, CancellationToken cancellationToken)
        {
            Event eventRecord = await work.Events.GetAsync(id, cancellationToken: cancellationToken);
            EventViewModel eventView = new EventViewModel(eventRecord);
            return eventView;
        }

        public async Task<EventViewModel> CreateEvent(EventCreateModel eventCreate, CancellationToken cancellationToken)
        {
            Event newEvent = new Event
            {
                Title = eventCreate.Title,
                Content = eventCreate.Content,
                Status = EventStatus.ACTIVE,
            };

            await work.Events.InsertAsync(newEvent, cancellationToken:cancellationToken);
            var result = await work.SaveChangesAsync(cancellationToken:cancellationToken);
            EventViewModel eventView = new EventViewModel(newEvent);
            if (result > 0)
            {
                return eventView;
            }
            return null!;
        }

        public async Task<EventViewModel> UpdateEvent(Guid id, EventUpdateModel eventUpdate)
        {
            var currentEvent = await work.Events.GetAsync(id);
            if (currentEvent != null)
            {
                if(eventUpdate.Title != null) currentEvent.Title = eventUpdate.Title;
                if(eventUpdate.Content != null) currentEvent.Content = eventUpdate.Content;
                if(eventUpdate.Status != null) currentEvent.Status = (EventStatus)eventUpdate.Status;
            }

            await work.Events.UpdateAsync(currentEvent!);
            await work.SaveChangesAsync();
            EventViewModel eventView = new EventViewModel(currentEvent);
            return eventView;
        }
    }
}
