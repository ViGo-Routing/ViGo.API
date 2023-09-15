//using ViGo.Domain;
//using ViGo.Domain.Enumerations;
//using ViGo.Models.QueryString;
//using ViGo.Models.QueryString.Sorting;

//namespace ViGo.Models.Events
//{
//    public class EventViewModel
//    {
//        public Guid Id { get; set; }
//        public string Title { get; set; } = null!;
//        public string Content { get; set; } = null!;
//        public EventStatus Status { get; set; }
//        public DateTime StartDate { get; set; }
//        public DateTime EndDate { get; set; }

//        public DateTime CreatedTime { get; set; }
//        public Guid CreatedBy { get; set; }
//        public DateTime UpdatedTime { get; set; }
//        public Guid UpdatedBy { get; set; }
//        public bool IsDeleted { get; set; }

//        public EventViewModel(Event ev)
//        {
//            Id = ev.Id;
//            Title = ev.Title;
//            Content = ev.Content;
//            Status = ev.Status;
//            StartDate = ev.StartDate;
//            EndDate = ev.EndDate;
//            CreatedTime = ev.CreatedTime;
//            CreatedBy = ev.CreatedBy;
//            UpdatedTime = ev.UpdatedTime;
//            UpdatedBy = ev.UpdatedBy;
//            IsDeleted = ev.IsDeleted;
//        }
//    }

//    public class EventSortingParameters : SortingParameters
//    {
//        public EventSortingParameters()
//        {
//            OrderBy = QueryStringUtilities.ToSortingCriteria(
//                new SortingCriteria(nameof(Event.StartDate)));
//        }
//    }
//}
