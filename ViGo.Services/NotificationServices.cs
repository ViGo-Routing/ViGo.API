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
using ViGo.Models.Notifications;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Repository.Pagination;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Exceptions;
using ViGo.Utilities.Google.Firebase;
using ViGo.Utilities.Validator;

namespace ViGo.Services
{
    public class NotificationServices : BaseServices
    {
        public NotificationServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task<IPagedEnumerable<NotificationViewModel>>
            GetNotificationsAsync(Guid userId,
            PaginationParameter pagination,
            HttpContext context, 
            CancellationToken cancellationToken)
        {
            if (!IdentityUtilities.IsAdmin() && !IdentityUtilities.IsStaff()
                && !userId.Equals(IdentityUtilities.GetCurrentUserId())) 
            {
                throw new AccessDeniedException("Bạn không thể thực hiện hành động này!!");
            }

            IEnumerable<Notification> notifications = await work.Notifications
                .GetAllAsync(query => query.Where(
                    n => n.UserId.HasValue && n.UserId.Equals(userId)), cancellationToken: cancellationToken);

            int totalRecords = notifications.Count();

            notifications = notifications.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize).Data;

            IEnumerable<Guid> eventIds = notifications.Where(n => n.EventId.HasValue)
                .Select(n => n.EventId.Value).Distinct();
            IEnumerable<Event> events = await work.Events.GetAllAsync(query => query.Where(
                e => eventIds.Contains(e.Id)), cancellationToken: cancellationToken);

            IList<NotificationViewModel> models = new List<NotificationViewModel>();
            foreach (Notification notification in notifications)
            {
                EventViewModel? eventModel = null;
                if (notification.EventId.HasValue)
                {
                    Event notiEvent = events.SingleOrDefault(e => e.Id.Equals(notification.EventId.Value));
                    eventModel = new EventViewModel(notiEvent);
                }
                models.Add(new NotificationViewModel(notification, eventModel));
            }

            return models.ToPagedEnumerable(pagination.PageNumber,
                pagination.PageSize, totalRecords, context);
        }

        public async Task<NotificationViewModel>
            GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
        {
            Notification? notification = await work.Notifications.GetAsync(notificationId, cancellationToken: cancellationToken);
            if (notification is null)
            {
                throw new ApplicationException("Thông báo không tồn tại!!");
            }

            if (notification.UserId.HasValue)
            {
                // Belongs to a specific user
                // Only that user and Admin/Staff can retrieve
                if (!IdentityUtilities.IsAdmin() && !IdentityUtilities.IsStaff()
                    && !IdentityUtilities.GetCurrentUserId().Equals(notification.UserId.Value))
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!!");
                }
            }
            else
            {
                // No User
                // Only Admin or Staff can retrieve
                if (!IdentityUtilities.IsStaff() && !IdentityUtilities.IsAdmin())
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!!");
                }
            }

            UserViewModel? notiUser = null;
            EventViewModel? notiEvent = null;

            if (notification.UserId.HasValue)
            {
                User user = await work.Users.GetAsync(notification.UserId.Value, cancellationToken: cancellationToken);
                notiUser = new UserViewModel(user);
            }
            if (notification.EventId.HasValue)
            {
                Event ev = await work.Events.GetAsync(notification.EventId.Value, cancellationToken: cancellationToken);
                notiEvent = new EventViewModel(ev);
            }

            NotificationViewModel model = new NotificationViewModel(notification, notiEvent, notiUser);
            return model;
        }

        public async Task<Notification> DeleteNotificationAsync(Guid notificationId, CancellationToken cancellationToken)
        {
            Notification? notification = await work.Notifications.GetAsync(notificationId, cancellationToken: cancellationToken);
            if (notification is null)
            {
                throw new ApplicationException("Thông báo không tồn tại!!");
            }

            if (notification.UserId.HasValue)
            {
                // Belongs to a specific user
                // Only that user and Admin/Staff can delete
                if (!IdentityUtilities.IsAdmin() && !IdentityUtilities.IsStaff()
                    && !IdentityUtilities.GetCurrentUserId().Equals(notification.UserId.Value))
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!!");
                }
            } else
            {
                // No User
                // Only Admin or Staff can delete
                if (!IdentityUtilities.IsStaff() && !IdentityUtilities.IsAdmin())
                {
                    throw new AccessDeniedException("Bạn không được phép thực hiện hành động này!!");
                }
            }
            await work.Notifications.DeleteAsync(notification);
            await work.SaveChangesAsync(cancellationToken);

            return notification;
        }

        public async Task<Notification> CreateNotificationAsync(NotificationCreateModel model,
            CancellationToken cancellationToken)
        {
            model.Title.StringValidate(
                allowEmpty: false,
                emptyErrorMessage: "Tiêu đề thông báo không được bỏ trống!",
                minLength: 5,
                minLengthErrorMessage: "Tiêu đề phải có ít nhất 5 kí tự!",
                maxLength: 255,
                maxLengthErrorMessage: "Tiêu đề không được vượt quá 255 kí tự!"
                );
            model.Description.StringValidate(
               allowEmpty: false,
               emptyErrorMessage: "Nội dung thông báo không được bỏ trống!",
               minLength: 5,
               minLengthErrorMessage: "Nội dung phải có ít nhất 5 kí tự!",
               maxLength: 500,
               maxLengthErrorMessage: "Nội dung không được vượt quá 255 kí tự!"
               );

            if (!Enum.IsDefined(model.Type))
            {
                throw new ApplicationException("Loại thông báo không hợp lệ!!");
            }

            if (model.UserId.HasValue)
            {
                User? checkUser = await work.Users.GetAsync(model.UserId.Value, cancellationToken: cancellationToken);
                if (checkUser is null)
                {
                    throw new ApplicationException("Thông tin người dùng không hợp lệ!!");
                }
            }
            if (model.EventId.HasValue)
            {
                Event? checkEvent = await work.Events.GetAsync(model.EventId.Value, cancellationToken: cancellationToken);
                if (checkEvent is null)
                {
                    throw new ApplicationException("Thông tin sự kiện không hợp lệ!!");
                }
            }

            Notification notification = new Notification
            {
                Title = model.Title,
                Description = model.Description,
                Type = model.Type,
                UserId = model.UserId,
                EventId = model.EventId,
                Status = NotificationStatus.ACTIVE,
            };
            await work.Notifications.InsertAsync(notification, cancellationToken: cancellationToken);
            await work.SaveChangesAsync(cancellationToken);

            if (model.IsSentToUser && model.UserId.HasValue)
            {
                // Send Push Notification to user
                User? user = await work.Users.GetAsync(model.UserId.Value, cancellationToken: cancellationToken);
                if (user is null)
                {
                    throw new ApplicationException("Thông tin người dùng không tồn tại!!");
                }

                string? fcmToken = user.FcmToken;
                if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                {
                    await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken,
                        model.Title, model.Description, cancellationToken: cancellationToken);
                }
            }

            return notification;
        }

        internal async Task<Notification> CreateFirebaseNotificationAsync(
            NotificationCreateModel model, CancellationToken cancellationToken)
        {
            Notification notification = new Notification
            {
                Title = model.Title,
                Description = model.Description,
                Type = model.Type,
                UserId = model.UserId,
                EventId = model.EventId,
                Status = NotificationStatus.ACTIVE,
                CreatedBy = model.UserId.Value,
                UpdatedBy = model.UserId.Value
            };
            await work.Notifications.InsertAsync(notification, cancellationToken: cancellationToken);
            await work.SaveChangesAsync(cancellationToken);

            //if (model.IsSentToUser && model.UserId.HasValue)
            //{
            //    // Send Push Notification to user
            //    User? user = await work.Users.GetAsync(model.UserId.Value, cancellationToken: cancellationToken);
            //    if (user is null)
            //    {
            //        throw new ApplicationException("Thông tin người dùng không tồn tại!!");
            //    }

            //    string? fcmToken = user.FcmToken;
            //    if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
            //    {
            //        await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken,
            //            model.Title, model.Description, cancellationToken: cancellationToken);
            //    }
            //}

            return notification;
        }
    }
}
