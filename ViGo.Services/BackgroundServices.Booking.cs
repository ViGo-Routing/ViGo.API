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

                    await work.Users.UpdateAsync(user);
                    await work.SaveChangesAsync(cancellationToken);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }

        }

        public async Task TripWasCompletedHandlerAsync(Guid bookingDetailId,
            Guid customerId, CancellationToken cancellationToken)
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
                Wallet customerWallet = await work.Wallets.GetAsync(
                    w => w.UserId.Equals(customer.Id), cancellationToken: cancellationToken);

                WalletTransaction customerTransaction_Withdrawal = new WalletTransaction
                {
                    WalletId = customerWallet.Id,
                    Amount = bookingDetail.Price.Value,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_PAID,
                    Status = WalletTransactionStatus.PENDING
                };
                if (customerWallet.Balance >= bookingDetail.PriceAfterDiscount.Value)
                {
                    customerTransaction_Withdrawal.Status = WalletTransactionStatus.SUCCESSFULL;

                    customerWallet.Balance -= bookingDetail.PriceAfterDiscount.Value;

                    //isCustomerPaymentSuccessful = true;
                    bookingDetail.Status = BookingDetailStatus.COMPLETED;

                } else
                {
                    bookingDetail.Status = BookingDetailStatus.PENDING_PAID;
                }


                // Get SYSTEM WALLET
                Wallet systemWallet = await work.Wallets.GetAsync(w =>
                    w.Type == WalletType.SYSTEM, cancellationToken: cancellationToken);
                if (systemWallet is null)
                {
                    throw new Exception("Chưa có ví dành cho hệ thống!!");
                }

                WalletTransaction systemTransaction_Add = new WalletTransaction
                {
                    WalletId = systemWallet.Id,
                    Amount = driverWage,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_PAID,
                    Status = WalletTransactionStatus.SUCCESSFULL,
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
                    Amount = driverWage,
                    BookingDetailId = bookingDetail.Id,
                    Type = WalletTransactionType.TRIP_INCOME,
                    Status = WalletTransactionStatus.SUCCESSFULL
                };

                systemWallet.Balance -= driverWage;
                driverWallet.Balance += driverWage;

                await work.WalletTransactions.InsertAsync(customerTransaction_Withdrawal, cancellationToken: cancellationToken);
                await work.WalletTransactions.InsertAsync(systemTransaction_Add, cancellationToken: cancellationToken);
                await work.WalletTransactions.InsertAsync(driverTransaction_Add, cancellationToken: cancellationToken);

                await work.Wallets.UpdateAsync(customerWallet);
                await work.Wallets.UpdateAsync(systemWallet);
                await work.Wallets.UpdateAsync(driverWallet);

                await work.BookingDetails.UpdateAsync(bookingDetail);

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

                if (customerTransaction_Withdrawal.Status == WalletTransactionStatus.SUCCESSFULL)
                {
                    customerPaymentNotification.Title = "Thực hiện thanh toán cho chuyến đi thành công!";
                    customerPaymentNotification.Description = "Thanh toán " + customerTransaction_Withdrawal.Amount
                        + "đ cho chuyến đi thành công!";
                } else if (customerTransaction_Withdrawal.Status == WalletTransactionStatus.PENDING)
                {
                    customerPaymentNotification.Title = "Không thể thực hiện thanh toán cho chuyến đi!";
                    customerPaymentNotification.Description = "Số dư ví của bạn không đủ để thanh toán " + customerTransaction_Withdrawal.Amount
                        + "đ cho chuyến đi! Vui lòng thực hiện nạp tiền vào ví để thanh toán cho chuyến đi!";
                }

                customerPaymentDataToSend.Add("walletTransactionId", customerTransaction_Withdrawal.Id.ToString());

                if (driverTransaction_Add.Status == WalletTransactionStatus.SUCCESSFULL)
                {
                    driverPaymentNotification.Title = "Nhận tiền công cho chuyến đi thành công!";
                    driverPaymentNotification.Description = "Tiền công " + driverTransaction_Add.Amount
                        + "đ cho chuyến đi đã được chuyển vào ví của bạn thành công!";
                }
                else if (driverTransaction_Add.Status == WalletTransactionStatus.PENDING)
                {
                    driverPaymentNotification.Title = "Không thể thực hiện nhận tiền công cho chuyến đi!";
                    driverPaymentNotification.Description = "Tiền công " + driverTransaction_Add.Amount
                        + "đ cho chuyến đi chưa được chuyển vào ví của bạn!";
                }
                driverPaymentDataToSend.Add("walletTransactionId", driverTransaction_Add.Id.ToString());

                if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
                {
                    await notificationServices.CreateFirebaseNotificationAsync(
                        customerPaymentNotification, customerFcm, customerPaymentDataToSend, cancellationToken);
                }
                if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
                {
                    await notificationServices.CreateFirebaseNotificationAsync(
                        driverPaymentNotification, driverFcm, driverPaymentDataToSend, cancellationToken);
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }
    }
}
