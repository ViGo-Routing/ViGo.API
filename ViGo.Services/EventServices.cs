using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Events;
using ViGo.Models.QueryString;
using ViGo.Models.QueryString.Pagination;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class EventServices : BaseServices
    {
        public EventServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<EventViewModel>> GetAllEvents(
            PaginationParameter pagination, EventSortingParameters sorting,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Event> events = await work.Events.GetAllAsync(cancellationToken: cancellationToken);

            events = events.Sort(sorting.OrderBy);

            int totalRecords = events.Count();

            events = events.ToPagedEnumerable(
                pagination.PageNumber, pagination.PageSize).Data;

            IEnumerable<EventViewModel> eventViews = from eventView in events
                                                     orderby eventView.Status descending
                                                     select new EventViewModel(eventView);

            return eventViews.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<IPagedEnumerable<EventViewModel>> GetAllActiveEvents(
            PaginationParameter pagination, EventSortingParameters sorting,
            HttpContext context,
            CancellationToken cancellationToken)
        {
            IEnumerable<Event> events = await work.Events.GetAllAsync(
                q => q.Where(x => x.Status.Equals(EventStatus.ACTIVE)), cancellationToken: cancellationToken);

            events = events.Sort(sorting.OrderBy);

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
            //eventCreate.StartDate.DateTimeValidate(minimum: DateTimeUtilities.GetDateTimeVnNow(), minErrorMessage: "Ngày bắt đầu không hợp lệ!");
            eventCreate.EndDate.DateTimeValidate(minimum: eventCreate.StartDate, minErrorMessage: "Ngày kết thúc phải sau ngày bắt đầu!");

            Event newEvent = new Event
            {
                Title = eventCreate.Title,
                Content = eventCreate.Content,
                StartDate = eventCreate.StartDate,
                EndDate = eventCreate.EndDate,
                Status = EventStatus.ACTIVE,
            };

            await work.Events.InsertAsync(newEvent, cancellationToken: cancellationToken);
            var result = await work.SaveChangesAsync(cancellationToken: cancellationToken);
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
            if (currentEvent == null)
            {
                throw new ApplicationException("Event không tồn tại!");
            }
            if (currentEvent != null)
            {
                if (eventUpdate.Title != null) currentEvent.Title = eventUpdate.Title;
                if (eventUpdate.Content != null) currentEvent.Content = eventUpdate.Content;
                if (eventUpdate.StartDate != null)
                {
                    eventUpdate.StartDate.Value.DateTimeValidate(maximum: eventUpdate.EndDate, maxErrorMessage: "Ngày bắt đầu phải trước ngày kết thúc!");
                    currentEvent.StartDate = (DateTime)eventUpdate.StartDate;
                }
                if (eventUpdate.EndDate != null)
                {
                    eventUpdate.EndDate.Value.DateTimeValidate(minimum: eventUpdate.StartDate, minErrorMessage: "Ngày kết thúc phải sau ngày bắt đầu!");
                    currentEvent.EndDate = (DateTime)eventUpdate.EndDate;
                }
                if (eventUpdate.Status != null) currentEvent.Status = (EventStatus)eventUpdate.Status;
            }

            await work.Events.UpdateAsync(currentEvent!);
            await work.SaveChangesAsync();
            EventViewModel eventView = new EventViewModel(currentEvent!);
            return eventView;
        }
    }
}
