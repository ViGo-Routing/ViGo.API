using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.CronJobs;
using ViGo.Models.Notifications;
using ViGo.Repository;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities.BackgroundTasks;

namespace ViGo.Services
{
    public class CronJobServices : UseNotificationServices
    {
        public CronJobServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task ResetUserWeeklyCancelRateAsync(CancellationToken cancellationToken)
        {
            IEnumerable<User> users = await work.Users.GetAllAsync(cancellationToken: cancellationToken);
            foreach (User user in users)
            {
                user.WeeklyCanceledTripRate = 0;
                await work.Users.UpdateAsync(user, isManuallyAssignTracking: true);
            }

            await work.SaveChangesAsync(cancellationToken);
        }

        public async Task RemindForTripAsync(Guid bookingDetailId,
            CancellationToken cancellationToken)
        {
            BookingDetail bookingDetail = await work.BookingDetails
                .GetAsync(bookingDetailId, cancellationToken: cancellationToken);
            if (bookingDetail is null)
            {
                throw new Exception("Booking Detail does not exist, Id: " + bookingDetailId);
            }

            if (bookingDetail.Status != BookingDetailStatus.PENDING_ASSIGN
                && bookingDetail.Status != BookingDetailStatus.ASSIGNED)
            {
                throw new Exception("Booking Detail Status is not valid! Id: " + bookingDetailId
                    + "; Status: " + bookingDetail.Status.ToString());
            }

            Booking booking = await work.Bookings
                .GetAsync(bookingDetail.BookingId, cancellationToken: cancellationToken);

            // Send notification to Customer
            Guid customerId = booking.CustomerId;
            User customer = await work.Users.GetAsync(customerId, cancellationToken: cancellationToken);
            string? customerFcm = customer.FcmToken;

            Dictionary<string, string> dataToSend = new Dictionary<string, string>()
            {
                {"action", NotificationAction.BookingDetail },
                { "bookingDetailId", bookingDetail.Id.ToString() },
            };

            if (customerFcm != null && !string.IsNullOrEmpty(customerFcm))
            {
                NotificationCreateModel customerNotification = new NotificationCreateModel()
                {
                    UserId = customer.Id,
                    Title = "Sắp đến giờ di chuyển",
                    Description = $"Bạn có một chuyến đi vào lúc {bookingDetail.CustomerDesiredPickupTime.ToString(@"hh\:mm")} " +
                    $"ngày {bookingDetail.Date.ToString("dd/MM/yyyy")}! Hãy đến điểm đón đúng giờ nhé!",
                    Type = NotificationType.SPECIFIC_USER
                };
                await notificationServices.CreateFirebaseNotificationAsync(
                    customerNotification, customerFcm, dataToSend, cancellationToken);
            }

            if (bookingDetail.DriverId.HasValue)
            {
                User driver = await work.Users.GetAsync(bookingDetail.DriverId.Value, cancellationToken: cancellationToken);
                string? driverFcm = driver.FcmToken;

                if (driverFcm != null && !string.IsNullOrEmpty(driverFcm))
                {
                    NotificationCreateModel driverNotification = new NotificationCreateModel()
                    {
                        UserId = driver.Id,
                        Title = "Sắp đến giờ di chuyển",
                        Description = $"Bạn có một chuyến đi vào lúc {bookingDetail.CustomerDesiredPickupTime.ToString(@"hh\:mm")} " +
                        $"ngày {bookingDetail.Date.ToString("dd/MM/yyyy")}! Hãy đến điểm đón đúng giờ nhé!",
                        Type = NotificationType.SPECIFIC_USER
                    };
                    await notificationServices.CreateFirebaseNotificationAsync(
                        driverNotification, driverFcm, dataToSend, cancellationToken);
                }
            }

        }

        public async Task CheckForTopupTransactionStatus(Guid transactionId,
            string clientIpAddress, IBackgroundTaskQueue backgroundTaskQueue,
            IServiceScopeFactory serviceScopeFactory, HttpClient httpClient,
            CancellationToken cancellationToken)
        {
            WalletTransaction walletTransaction = await work.WalletTransactions
                .GetAsync(transactionId, cancellationToken: cancellationToken);

            if (walletTransaction is null)
            {
                throw new Exception("Wallet Transaction does not exist! ID: " + transactionId);
            }

            if (walletTransaction.Status == WalletTransactionStatus.PENDING)
            {
                PaymentServices paymentServices = new PaymentServices(work, _logger);
                var queryResponse = await paymentServices.GetVnPayTransactionStatus(
                    transactionId, httpClient, clientIpAddress, cancellationToken);

                if (queryResponse is null)
                {
                    throw new Exception("Transaction has no status! ID: " + transactionId);
                }

                //walletTransaction.Status = WalletTransactionStatus.SUCCESSFULL;

                //Wallet wallet = await work.Wallets.GetAsync(walletTransaction.WalletId,
                //    cancellationToken: cancellationToken);
                //wallet.Balance += walletTransaction.Amount;
                //await work.Wallets.UpdateAsync(wallet, isManuallyAssignTracking: true);

                Wallet wallet = await work.Wallets.GetAsync(walletTransaction.WalletId,
                    cancellationToken: cancellationToken);
                //if (wallet is null)
                //{
                //    throw new ApplicationException("Không tìm thấy ví của người dùng!");
                //}

                if (queryResponse.ResponseCode == "00" && queryResponse.TransactionStatus == "00")
                {
                    //systemTransaction_Add.Status = WalletTransactionStatus.SUCCESSFULL;
                    //systemTransaction_Add.Amount += vnpAmount;

                    walletTransaction.ExternalTransactionId += "_VnPay_" + queryResponse.TransactionNo;

                    walletTransaction.Status = WalletTransactionStatus.SUCCESSFULL;
                    wallet.Balance += walletTransaction.Amount;

                    // TODO Code
                    // Check for Failed Canceled Fee withdrawal

                    _logger.LogInformation("Topup has been paid successfully!! WalletTransactionId={0}, VNPay TransactionId={1}",
                        walletTransaction.Id, queryResponse.TransactionNo);

                }
                else
                {
                    //throw new ApplicationException("Thanh toán VNPay lỗi! Mã lỗi: " + vnpResponseCode);
                    //systemTransaction_Add.Status = WalletTransactionStatus.FAILED;
                    //walletTransaction_Topup.Status = WalletTransactionStatus.FAILED;
                    //walletTransaction_Paid.Status = WalletTransactionStatus.FAILED;

                    //booking.PaymentMethod = PaymentMethod.VNPAY;
                    walletTransaction.Status = WalletTransactionStatus.FAILED;

                    _logger.LogInformation("Topup failed to be paid!! " +
                        "WalletTransactionId={0}, VNPay TransactionId={1}, ResponseCode={2}",
                        walletTransaction.Id, queryResponse.TransactionNo, queryResponse.ResponseCode);
                }

                await work.WalletTransactions.UpdateAsync(walletTransaction, isManuallyAssignTracking: true);
                await work.Wallets.UpdateAsync(wallet, isManuallyAssignTracking: true);

                // Add Wallet Transaction
                //await work.WalletTransactions.InsertAsync(systemTransaction_Add,
                //    isManuallyAssignTracking: true, cancellationToken: cancellationToken);
                //await work.WalletTransactions.InsertAsync(walletTransaction_Topup,
                //    isManuallyAssignTracking: true, cancellationToken: cancellationToken);
                //await work.WalletTransactions.InsertAsync(walletTransaction_Paid,
                //    isManuallyAssignTracking: true, cancellationToken: cancellationToken);

                await work.SaveChangesAsync(cancellationToken);

                // TODO Code

                // Run trip mapping
                //await backgroundTaskQueue.QueueBackGroundWorkItemAsync(async token =>
                //{
                //    await using (var scope = serviceScopeFactory.CreateAsyncScope())
                //    {
                //        IUnitOfWork work = new UnitOfWork(scope.ServiceProvider);
                //        TripMappingServices tripMappingServices = new TripMappingServices(work, _logger);
                //        await tripMappingServices.MapBooking(booking, _logger);
                //    }
                //});

                // Send notification to user
                User user = await work.Users.GetAsync(wallet.UserId, cancellationToken: cancellationToken);

                string? fcmToken = user.FcmToken;

                //_logger.LogInformation("User FCM: " + fcmToken);
                NotificationCreateModel notification = new NotificationCreateModel()
                {
                    Type = NotificationType.SPECIFIC_USER,
                    UserId = user.Id
                };
                //notification.UserId = user.Id;

                //if (walletTransaction.Status == WalletTransactionStatus.SUCCESSFULL)
                //{
                //    returnUserId = user.Id;
                //}

                //await work.WalletTransactions.UpdateAsync(walletTransaction, isManuallyAssignTracking: true);
                //await work.SaveChangesAsync(cancellationToken);

                if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                {
                    //_logger.LogInformation("User Notification!!");
                    //_logger.LogInformation("Wallet Transaction Status: " + walletTransaction.Status.ToString());

                    if (walletTransaction.Status == WalletTransactionStatus.SUCCESSFULL)
                    {
                        Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                        {
                            {"action", NotificationAction.TransactionDetail },
                                { "walletTransactionId", walletTransaction.Id.ToString() },
                                { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                                { "isSuccess", "true" },
                                { "message", "Thanh toán bằng VNPay thành công!" }
                        };

                        notification.Title = "Thanh toán bằng VNPay thành công";
                        notification.Description = "Quý khách đã thực hiện thanh toán topup bằng VNPay thành công!!";

                        //await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thành công",
                        //"Quý khách đã thực hiện thanh toán topup bằng VNPay thành công!!", data: dataToSend,
                        //    cancellationToken: cancellationToken);

                        //// Send data to mobile application
                        //await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, dataToSend, cancellationToken);

                        await notificationServices.CreateFirebaseNotificationAsync(notification, fcmToken,
                            dataToSend, cancellationToken);
                    }
                    else
                    {
                        // FAILED
                        Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                        {
                            {"action", NotificationAction.TransactionDetail },
                            { "walletTransactionId", walletTransaction.Id.ToString() },
                            { "paymentMethod", PaymentMethod.VNPAY.ToString() },
                            { "isSuccess", "false" },
                            { "message", "Thanh toán bằng VNPay thất bại!" }
                        };

                        notification.Title = "Thanh toán bằng VNPay thất bại";
                        notification.Description = "Giao dịch thực hiện thanh toán topup bằng VNPay của quý khách đã bị thất bại!!";

                        // await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken, "Thanh toán bằng VNPay thất bại",
                        //"Giao dịch thực hiện thanh toán topup bằng VNPay của quý khách đã bị thất bại!!", data: dataToSend,
                        //cancellationToken: cancellationToken);

                        // // Send data to mobile application
                        // await FirebaseUtilities.SendDataToDeviceAsync(fcmToken, dataToSend, cancellationToken);

                        await notificationServices.CreateFirebaseNotificationAsync(
                            notification, fcmToken, dataToSend, cancellationToken);
                    }

                }

                if (walletTransaction.Status == WalletTransactionStatus.SUCCESSFULL)
                {
                    // Check for pending withdrawal transactions
                    await backgroundTaskQueue.QueueBackGroundWorkItemAsync(async token =>
                    {
                        await using (var scope = serviceScopeFactory.CreateAsyncScope())
                        {
                            IUnitOfWork unitOfWork = new UnitOfWork(scope.ServiceProvider);
                            BackgroundServices backgroundServices = new BackgroundServices(unitOfWork, _logger);
                            await backgroundServices.CheckForPendingTransactionsAsync(user.Id, token);
                        }
                    });
                }

            }
        }

        public async Task<IEnumerable<CronJobViewModel>>
            GetCronJobsAsync(IScheduler scheduler,
            CancellationToken cancellationToken)
        {
            IEnumerable<string> jobGroups = await scheduler.GetJobGroupNames(cancellationToken);

            IList<CronJobViewModel> results = new List<CronJobViewModel>();

            foreach (string group in jobGroups)
            {
                var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                var jobKeys = await scheduler.GetJobKeys(groupMatcher, cancellationToken);

                IList<JobViewModel> jobs = new List<JobViewModel>();
                foreach (var jobKey in jobKeys)
                {
                    var detail = await scheduler.GetJobDetail(jobKey, cancellationToken);
                    var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken);

                    IList<TriggerViewModel> triggerList = new List<TriggerViewModel>();
                    foreach (var trigger in triggers)
                    {
                        var triggerState = await scheduler.GetTriggerState(trigger.Key, cancellationToken);
                        var nextFireTime = trigger.GetNextFireTimeUtc();
                        var previousFireTime = trigger.GetPreviousFireTimeUtc();

                        triggerList.Add(new TriggerViewModel
                        {
                            TriggerKey = trigger.Key.Name,
                            TriggerState = triggerState.ToString(),
                            NextFireTimeUtc = nextFireTime,
                            PreviousFireTimeUtc = previousFireTime
                        });
                    }

                    jobs.Add(new JobViewModel
                    {
                        JobKey = jobKey.Name,
                        JobDescription = detail.Description,
                        Triggers = triggerList
                    });
                }

                results.Add(new CronJobViewModel
                {
                    JobGroup = group,
                    Jobs = jobs
                });
            }
            return results;
        }
    }
}
