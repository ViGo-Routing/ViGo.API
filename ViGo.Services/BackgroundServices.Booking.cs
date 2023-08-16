using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;
using ViGo.Domain;
using ViGo.Models.Notifications;
using ViGo.Utilities.Extensions;
using Quartz;
using ViGo.Utilities.CronJobs;
using ViGo.Utilities;

namespace ViGo.Services
{
    public partial class BackgroundServices
    {
        public async Task CalculateTripCancelRateAsync(Guid userId,
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
                }
                else if (user.Role == UserRole.DRIVER)
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


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }

        public async Task CalculateWeeklyTripCancelRateAsync(Guid userId,
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

        public async Task CalculateDriverRatingAsync(Guid driverId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - CALCULATE DRIVER RATING ======");
            _logger.LogInformation("====== DriverId: {0} ======", driverId);
            try
            {
                User? user = await work.Users.GetAsync(driverId,
                cancellationToken: cancellationToken);
                if (user is null ||
                    user.Role != UserRole.DRIVER)
                {
                    throw new ApplicationException("Invalid User!!");
                }

                // Get All ArriveAtDropOff/Completed Booking Details
                // And have Rating
                IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                    .GetAllAsync(query => query.Where(
                        d => d.DriverId.HasValue && d.DriverId.Equals(driverId)
                        && (d.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF
                        || d.Status == BookingDetailStatus.COMPLETED)
                        && d.Rate.HasValue
                        ), cancellationToken: cancellationToken);

                if (bookingDetails.Any())
                {
                    double avgRating = Math.Round(
                    bookingDetails.Average(d => d.Rate.Value), 2);

                    _logger.LogInformation("Driver's Rating: " + avgRating);

                    user.Rating = avgRating;

                    await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
                    await work.SaveChangesAsync(cancellationToken);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }

        }

        public async Task TripWasCompletedHandlerAsync(Guid bookingDetailId,
            Guid customerId, bool isCanceledByBooker = false,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("====== BEGIN TASK - TRIP WAS COMPLETED HANDLER ======");
            _logger.LogInformation("====== BookingDetailId: {0} ======", bookingDetailId);
            try
            {
                BookingDetail? bookingDetail = await work.BookingDetails.GetAsync(
                    bookingDetailId, cancellationToken: cancellationToken);

                if (bookingDetail is null)
                {
                    throw new ApplicationException("Booking Detail does not exist!!");
                }

                User? customer = await work.Users.GetAsync(customerId,
                    cancellationToken: cancellationToken);
                if (customer is null || customer.Role != UserRole.CUSTOMER)
                {
                    throw new ApplicationException("User does not exist!!");
                }

                if (!bookingDetail.DriverId.HasValue || bookingDetail.Status != BookingDetailStatus.ARRIVE_AT_DROPOFF)
                {
                    throw new ApplicationException("Booking Detail status is not valid!!");
                }

                //bool isCustomerPaymentSuccessful = false;
                //bool isDriverPaymentSuccessful = false;
                NotificationCreateModel customerPaymentNotification = new NotificationCreateModel()
                {
                    UserId = customer.Id,
                    Type = NotificationType.SPECIFIC_USER
                };
                Dictionary<string, string> customerPaymentDataToSend = new Dictionary<string, string>()
                {
                    {"action", NotificationAction.TransactionDetail },
                };

                NotificationCreateModel driverPaymentNotification = new NotificationCreateModel()
                {
                    UserId = bookingDetail.DriverId.Value,
                    Type = NotificationType.SPECIFIC_USER
                };

                Dictionary<string, string> driverPaymentDataToSend = new Dictionary<string, string>()
                {
                    {"action", NotificationAction.TransactionDetail },
                };

                //if (updateDto.Status == BookingDetailStatus.ARRIVE_AT_DROPOFF)
                //{
                // Calculate driver wage
                FareServices fareServices = new FareServices(work, _logger);

                if (!bookingDetail.PriceAfterDiscount.HasValue
                    || !bookingDetail.Price.HasValue)
                {
                    throw new ApplicationException("Booking Detail's information is not valid!");
                }

                double driverWage = await fareServices.CalculateDriverWage(
                    bookingDetail.Price.Value, cancellationToken);

                // Get Customer Wallet
                //Wallet customerWallet = await work.Wallets.GetAsync(
                //    w => w.UserId.Equals(customer.Id), cancellationToken: cancellationToken);

                //WalletTransaction customerTransaction_Withdrawal = new WalletTransaction
                //{
                //    WalletId = customerWallet.Id,
                //    Amount = bookingDetail.Price.Value,
                //    BookingDetailId = bookingDetail.Id,
                //    Type = WalletTransactionType.TRIP_PAID,
                //    Status = WalletTransactionStatus.PENDING
                //};
                //if (customerWallet.Balance >= bookingDetail.PriceAfterDiscount.Value)
                //{
                //    customerTransaction_Withdrawal.Status = WalletTransactionStatus.SUCCESSFULL;

                //    customerWallet.Balance -= bookingDetail.PriceAfterDiscount.Value;

                //    //isCustomerPaymentSuccessful = true;
                //    bookingDetail.Status = BookingDetailStatus.COMPLETED;

                //} else
                //{
                //    bookingDetail.Status = BookingDetailStatus.PENDING_PAID;
                //}

                // Get Driver Wallet
                //Wallet driverWallet = await work.Wallets.GetAsync(
                //    w => w.User.Equals(bookingDetail.DriverId.Value), cancellationToken: cancellationToken);
                //WalletTransaction driverTransaction = new WalletTransaction
                //{
                //    WalletId = driverWallet.Id,
                //    Amount = bookingDetail.Price.Value,
                //    BookingDetailId = bookingDetail.Id,
                //    Type = WalletTransactionType.TRIP_INCOME,
                //    Status = WalletTransactionStatus.SUCCESSFULL,
                //    PaymentMethod = PaymentMethod.WALLET,
                //};

                // Get SYSTEM WALLET
                Wallet systemWallet = await work.Wallets.GetAsync(w =>
                    w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);
                if (systemWallet is null)
                {
                    throw new Exception("Chưa có ví dành cho hệ thống!!");
                }

                WalletTransaction systemTransaction_Withdrawal = new WalletTransaction
                {
                    WalletId = systemWallet.Id,
                    Amount = bookingDetail.Price.Value,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_PAID,
                    Status = WalletTransactionStatus.SUCCESSFULL,
                    PaymentMethod = PaymentMethod.WALLET,
                    CreatedBy = bookingDetail.DriverId.Value,
                    UpdatedBy = bookingDetail.DriverId.Value
                };

                Wallet driverWallet = await work.Wallets.GetAsync(w =>
                    w.UserId.Equals(bookingDetail.DriverId.Value), cancellationToken: cancellationToken);
                if (driverWallet is null)
                {
                    throw new ApplicationException("Tài xế chưa được cấu hình ví!!");
                }

                WalletTransaction driverTransaction_Add = new WalletTransaction
                {
                    WalletId = driverWallet.Id,
                    Amount = bookingDetail.Price.Value,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_INCOME,
                    Status = WalletTransactionStatus.SUCCESSFULL,
                    PaymentMethod = PaymentMethod.WALLET,
                    CreatedBy = bookingDetail.DriverId.Value,
                    UpdatedBy = bookingDetail.DriverId.Value
                };

                systemWallet.Balance -= bookingDetail.Price.Value;
                driverWallet.Balance += bookingDetail.Price.Value;

                //await work.WalletTransactions.InsertAsync(customerTransaction_Withdrawal, cancellationToken: cancellationToken);
                await work.WalletTransactions.InsertAsync(systemTransaction_Withdrawal, isManuallyAssignTracking: true,
                    cancellationToken: cancellationToken);
                await work.WalletTransactions.InsertAsync(driverTransaction_Add, isManuallyAssignTracking: true,
                    cancellationToken: cancellationToken);

                //await work.Wallets.UpdateAsync(customerWallet);
                await work.Wallets.UpdateAsync(systemWallet, isManuallyAssignTracking: true);
                await work.Wallets.UpdateAsync(driverWallet, isManuallyAssignTracking: true);

                if (!isCanceledByBooker)
                {
                    bookingDetail.Status = BookingDetailStatus.COMPLETED;
                    await work.BookingDetails.UpdateAsync(bookingDetail, isManuallyAssignTracking: true);
                }

                //isDriverPaymentSuccessful = true;
                //}

                await work.SaveChangesAsync(cancellationToken);

                string? customerFcm = customer.FcmToken;

                User? driver = await work.Users.GetAsync(bookingDetail.DriverId.Value,
                    cancellationToken: cancellationToken);

                if (driver is null || driver.Role != UserRole.DRIVER)
                {
                    throw new ApplicationException("Driver does not exist!!");
                }

                string? driverFcm = driver.FcmToken;

                //if (customerTransaction_Withdrawal.Status == WalletTransactionStatus.SUCCESSFULL)
                //{
                //    customerPaymentNotification.Title = "Thực hiện thanh toán cho chuyến đi thành công!";
                //    customerPaymentNotification.Description = "Thanh toán " + customerTransaction_Withdrawal.Amount
                //        + "đ cho chuyến đi thành công!";
                //} else if (customerTransaction_Withdrawal.Status == WalletTransactionStatus.PENDING)
                //{
                //    customerPaymentNotification.Title = "Không thể thực hiện thanh toán cho chuyến đi!";
                //    customerPaymentNotification.Description = "Số dư ví của bạn không đủ để thanh toán " + customerTransaction_Withdrawal.Amount
                //        + "đ cho chuyến đi! Vui lòng thực hiện nạp tiền vào ví để thanh toán cho chuyến đi!";
                //}

                //customerPaymentDataToSend.Add("walletTransactionId", customerTransaction_Withdrawal.Id.ToString());

                //if (driverTransaction_Add.Status == WalletTransactionStatus.SUCCESSFULL)
                //{
                driverPaymentNotification.Title = "Nhận tiền công cho chuyến đi thành công!";
                driverPaymentNotification.Description = "Tiền công " + driverTransaction_Add.Amount
                    + "đ cho chuyến đi đã được chuyển vào ví của bạn thành công!";
                //}
                //else if (driverTransaction_Add.Status == WalletTransactionStatus.PENDING)
                //{
                //    driverPaymentNotification.Title = "Không thể thực hiện nhận tiền công cho chuyến đi!";
                //    driverPaymentNotification.Description = "Tiền công " + driverTransaction_Add.Amount
                //        + "đ cho chuyến đi chưa được chuyển vào ví của bạn!";
                //}
                driverPaymentDataToSend.Add("walletTransactionId", driverTransaction_Add.Id.ToString());

                //if (customerFcm != null && !string.IsNullOrEmpty(customerFcm) )
                //{
                //    await notificationServices.CreateFirebaseNotificationAsync(
                //        customerPaymentNotification, customerFcm, customerPaymentDataToSend, cancellationToken);
                //}
                if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
                {
                    await notificationServices.CreateFirebaseNotificationAsync(
                        driverPaymentNotification, driverFcm, driverPaymentDataToSend, cancellationToken);
                }

                // Check for Booking completed status
                _logger.LogInformation("Checking for Booking's completed status...");
                Booking booking = await work.Bookings.GetAsync(bookingDetail.BookingId,
                    cancellationToken: cancellationToken);
                IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails.GetAllAsync(
                    query => query.Where(
                        d => d.BookingId.Equals(booking.Id)), cancellationToken: cancellationToken);

                _logger.LogInformation("Booking Details Count: {0}", bookingDetails.Count());
                IEnumerable<BookingDetail> notCanceledBookingDetails = bookingDetails.Where(d => d.Status != BookingDetailStatus.CANCELLED);
                _logger.LogInformation("NOT CANCELED Booking Details Count: {0}", notCanceledBookingDetails.Count());

                if (notCanceledBookingDetails
                    .All(d => d.Status == BookingDetailStatus.COMPLETED))
                {
                    _logger.LogInformation("Every Booking Detail is competed!");
                    booking.Status = BookingStatus.COMPLETED;
                    await work.Bookings.UpdateAsync(booking, isManuallyAssignTracking: true);
                    await work.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Completed Booking Detail: {0}",
                        notCanceledBookingDetails.Count(d => d.Status == BookingDetailStatus.COMPLETED));
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }

        public async Task ScheduleTripReminderAsync(Guid bookingId,
            IScheduler scheduler,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - SCHEDULE TRIP REMINDER ======");
            _logger.LogInformation("====== BookingId: {0} ======", bookingId);
            try
            {
                IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                    .GetAllAsync(query => query.Where(
                        d => d.BookingId.Equals(bookingId)), cancellationToken: cancellationToken);

                if (!bookingDetails.Any())
                {
                    throw new Exception("No Booking Detail to schedule!! ID: " + bookingId);
                }

                JobKey tripReminderJobKey = new JobKey(CronJobIdentities.UPCOMING_TRIP_NOTIFICATION_JOBKEY);

                foreach (BookingDetail bookingDetail in bookingDetails)
                {
                    _logger.LogInformation("Trying to schedule for BookingDetail: ID: " +
                        bookingDetail.Id + "; Pickup Time: " + bookingDetail.PickUpDateTimeString());

                    DateTimeOffset scheduleDatetimeOffset = bookingDetail.PickUpDateTimeOffset().AddHours(-2);

                    ITrigger trigger = TriggerBuilder.Create()
                        .ForJob(tripReminderJobKey)
                        .WithIdentity(CronJobIdentities.UPCOMING_TRIP_NOTIFICATION_TRIGGER_ID + "_" + bookingDetail.Id)
                        .WithDescription("Send notification to user about upcoming trip")
                        .UsingJobData(CronJobIdentities.BOOKING_DETAIL_ID_JOB_DATA, bookingDetail.Id)
                        .StartAt(scheduleDatetimeOffset)
                        .Build();

                    await scheduler.ScheduleJob(trigger);

                    _logger.LogInformation("Scheduled for BookingDetail: ID: " +
                        bookingDetail.Id + "; Schedule Time: " + scheduleDatetimeOffset);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }

        public async Task ScheduleTripsReminderOnStartupAsync(
            IScheduler scheduler,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - SCHEDULE TRIP REMINDER ON STARTUP ======");
            try
            {
                IEnumerable<BookingDetail> bookingDetails = await work.BookingDetails
                .GetAllAsync(query => query.Where(
                    d => (d.Status == BookingDetailStatus.PENDING_ASSIGN
                    || d.Status == BookingDetailStatus.ASSIGNED)
                    ), cancellationToken: cancellationToken);

                DateTime vnNow = DateTimeUtilities.GetDateTimeVnNow();
                _logger.LogInformation("VN Time: {0}", vnNow.ToString("dd/MM/yyyy HH:mm:ss"));


                IEnumerable<BookingDetail> futureBookingDetails = bookingDetails.Where(
                    d => (d.PickUpDateTime() - vnNow).TotalHours >= 2);
                IEnumerable<BookingDetail> nowBookingDetails = bookingDetails.Where(
                    d => {
                        var difference = d.PickUpDateTime() - vnNow;
                        return difference.TotalHours >= 0 && difference.TotalHours < 2;
                    });
                _logger.LogInformation("Future schedule Booking Details Count: {0}", futureBookingDetails.Count());
                _logger.LogInformation("Booking Details need to schedule now: {0}", nowBookingDetails.Count());

                JobKey tripReminderJobKey = new JobKey(CronJobIdentities.UPCOMING_TRIP_NOTIFICATION_JOBKEY);

                _logger.LogInformation("Scheduling for Future Booking Details...");

                foreach (BookingDetail bookingDetail in futureBookingDetails)
                {
                    _logger.LogInformation("\tTrying to schedule for BookingDetail: ID: " +
                        bookingDetail.Id + "; Pickup Time: " + bookingDetail.PickUpDateTimeString());

                    DateTimeOffset scheduleDatetimeOffset = bookingDetail.PickUpDateTimeOffset().AddHours(-2);

                    ITrigger trigger = TriggerBuilder.Create()
                        .ForJob(tripReminderJobKey)
                        .WithIdentity(CronJobIdentities.UPCOMING_TRIP_NOTIFICATION_TRIGGER_ID + "_" + bookingDetail.Id)
                        .WithDescription("Send notification to user about upcoming trip")
                        .UsingJobData(CronJobIdentities.BOOKING_DETAIL_ID_JOB_DATA, bookingDetail.Id)
                        .StartAt(scheduleDatetimeOffset)
                        .Build();

                    await scheduler.ScheduleJob(trigger);

                    _logger.LogInformation("\tScheduled for BookingDetail: ID: " +
                        bookingDetail.Id + "; Schedule Time: " + scheduleDatetimeOffset);
                }

                _logger.LogInformation("Scheduling for Now Booking Details...");

                foreach (BookingDetail bookingDetail in nowBookingDetails)
                {
                    _logger.LogInformation("\tTrying to schedule for BookingDetail: ID: " +
                        bookingDetail.Id + "; Pickup Time: " + bookingDetail.PickUpDateTimeString());

                    DateTimeOffset scheduleDatetimeOffset = bookingDetail.PickUpDateTimeOffset().AddHours(-2);

                    ITrigger trigger = TriggerBuilder.Create()
                        .ForJob(tripReminderJobKey)
                        .WithIdentity(CronJobIdentities.UPCOMING_TRIP_NOTIFICATION_TRIGGER_ID + "_" + bookingDetail.Id)
                        .WithDescription("Send notification to user about upcoming trip")
                        .UsingJobData(CronJobIdentities.BOOKING_DETAIL_ID_JOB_DATA, bookingDetail.Id)
                        //.StartAt(scheduleDatetimeOffset)
                        .StartNow()
                        .Build();

                    await scheduler.ScheduleJob(trigger);

                    _logger.LogInformation("\tScheduled for BookingDetail: ID: " +
                        bookingDetail.Id + "; Schedule Time: Start Now");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
            finally
            {
                _logger.LogInformation("====== FINISH TASK - SCHEDULE TRIP REMINDER ON STARTUP ======");
            }
        }
    }
}
