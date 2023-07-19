using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Notifications;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities.Extensions;

namespace ViGo.Services
{
    public class BackgroundServices : UseNotificationServices
    {
        public BackgroundServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task CalculateTripCancelRate(Guid userId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - CALCULATE TRIP CANCEL RATE ======");
            _logger.LogInformation("====== UserId: {0} ======", userId);

            try
            {
                User? user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
                if (user is null)
                {
                    throw new ApplicationException("User does not exist!!");
                }

                if (user.Role == UserRole.ADMIN)
                {
                    throw new ApplicationException("Invalid User Role - " + user.Role.ToString());
                }

                // Get number of canceled trips by the user
                IEnumerable<BookingDetail> canceledBookingDetailsByUser =
                    await work.BookingDetails.GetAllAsync(query => query.Where(
                        d => d.Status == BookingDetailStatus.CANCELLED
                        && d.CanceledUserId.HasValue &&
                        d.CanceledUserId.Equals(userId)), cancellationToken: cancellationToken);
                
                int canceledTripsCount = canceledBookingDetailsByUser.Count() - 10;

                if (canceledTripsCount <= 0)
                {
                    // No rate calculating for the first 10 Canceled Booking Details
                    return;
                }

                // Get total number of user's trips
                int totalTrips = 0;

                if (user.Role == UserRole.CUSTOMER)
                {
                    // Customer
                    // Booking.CustomerId => BookingDetail count
                    IEnumerable<Booking> customerBookings = await work.Bookings
                        .GetAllAsync(query => query.Where(
                            b => b.CustomerId.Equals(userId)), cancellationToken: cancellationToken);
                    IEnumerable<Guid> bookingIds = customerBookings.Select(b => b.Id);
                    IEnumerable<BookingDetail> totalBookingDetails = await work.BookingDetails
                        .GetAllAsync(query => query.Where(
                            d => bookingIds.Contains(d.BookingId)), cancellationToken: cancellationToken);

                    totalTrips = totalBookingDetails.Count();
                } else if (user.Role == UserRole.DRIVER)
                {
                    // Driver
                    // BookingDetail.DriverId
                    IEnumerable<BookingDetail> assignedBookingDetails = await work.BookingDetails
                        .GetAllAsync(query => query.Where(
                            d => d.DriverId.HasValue &&
                            d.DriverId.Value.Equals(userId)), cancellationToken: cancellationToken);

                    totalTrips = assignedBookingDetails.Count();
                }

                if (totalTrips == 0)
                {
                    throw new ApplicationException("Total number of User's Trips is invalid!!");
                }

                double cancelRate = Math.Round((double)canceledTripsCount / totalTrips, 4);

                _logger.LogInformation("User Cancel Rate: " + cancelRate);

                user.CanceledTripRate = cancelRate;

                if (cancelRate >= 0.8)
                {
                    // Account get banned
                    user.Status = UserStatus.BANNED;
                    _logger.LogInformation("User gets banned!");

                }

                await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                await work.SaveChangesAsync(cancellationToken);

                string? fcmToken = user.FcmToken;
                if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                {
                    if (cancelRate >= 0.8)
                    {
                        NotificationCreateModel notification = new NotificationCreateModel()
                        {
                            UserId = userId,
                            Type = NotificationType.SPECIFIC_USER,
                            Title = "Tài khoản của bạn đã bị khóa",
                            Description = "Tỉ lệ hủy chuyến của bạn đã vượt ngưỡng 80% nên tài khoản của bạn đã bị khóa"
                        };

                        Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                        {
                            { "action", NotificationAction.Profile },
                        };

                        await notificationServices.CreateFirebaseNotificationAsync(
                            notification, fcmToken, dataToSend, cancellationToken);
                    }
                    else if (cancelRate >= 0.7)
                    {
                        // Send notification for quota warning
                        NotificationCreateModel notification = new NotificationCreateModel()
                        {
                            UserId = userId,
                            Type = NotificationType.SPECIFIC_USER,
                            Title = "Tài khoản của bạn sắp bị khóa",
                            Description = "Tỉ lệ hủy chuyến của bạn sắp vượt ngưỡng 80%. Hãy cân nhắc về việc hủy chuyến " +
                            "trong tương lai, nếu không, tài khoản của bạn sẽ bị khóa!"
                        };

                        Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                        {
                            { "action", NotificationAction.Profile },
                        };

                        await notificationServices.CreateFirebaseNotificationAsync(
                            notification, fcmToken, dataToSend, cancellationToken);
                    }
                }
                

            } catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }

        public async Task CalculateWeeklyTripCancelRate(Guid userId,
            int inWeekCancelCount, CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - CALCULATE WEEKLY TRIP CANCEL RATE ======");
            _logger.LogInformation("====== UserId: {0} ======", userId);

            try
            {
                User? user = await work.Users.GetAsync(userId, cancellationToken: cancellationToken);
                if (user is null)
                {
                    throw new ApplicationException("User does not exist!!");
                }

                if (user.Role == UserRole.ADMIN)
                {
                    throw new ApplicationException("Invalid User Role - " + user.Role.ToString());
                }

                user.WeeklyCanceledTripRate += inWeekCancelCount;

                await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                await work.SaveChangesAsync(cancellationToken);

                string? fcmToken = user.FcmToken;
                if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                {
                    if (user.WeeklyCanceledTripRate >= 3)
                    {
                        NotificationCreateModel notification = new NotificationCreateModel()
                        {
                            UserId = userId,
                            Type = NotificationType.SPECIFIC_USER,
                            Title = "Số chuyến đi có thể hủy trong tuần của bạn đã đạt giới hạn",
                            Description = "Số chuyến đi đã bị bạn hủy có lịch trình trong tuần này đã đạt giới hạn! Bạn " +
                            "không thể hủy thêm chuyến đi nào có lịch trình trong tuần này nữa"
                        };

                        Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                        {
                            { "action", NotificationAction.Profile },
                        };

                        await notificationServices.CreateFirebaseNotificationAsync(
                            notification, fcmToken, dataToSend, cancellationToken);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }
    }
}
