using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Notifications;
using ViGo.Utilities;
using ViGo.Utilities.CronJobs;
using ViGo.Utilities.Extensions;

namespace ViGo.Services
{
    public partial class BackgroundServices
    {
        public async Task CheckForPendingTransactionsAsync(Guid userId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - CHECK PENDING TRANSACTIONS ======");
            _logger.LogInformation("====== UserId: {0} ======", userId);

            try
            {
                User? user = await work.Users.GetAsync(userId,
                    cancellationToken: cancellationToken);
                if (user is null)
                {
                    throw new ApplicationException("User does not exist!!");
                }

                Wallet? wallet = await work.Wallets.GetAsync(
                w => w.UserId.Equals(userId), cancellationToken: cancellationToken);
                if (wallet is null)
                {
                    throw new ApplicationException("Wallet does not exist!! UserId: " + userId);
                }

                IEnumerable<WalletTransaction> walletTransactions = await work.WalletTransactions
                    .GetAllAsync(query => query.Where(
                        t => t.WalletId.Equals(wallet.Id)
                        && t.Status == WalletTransactionStatus.PENDING
                        && (t.Type == WalletTransactionType.TRIP_PAID
                            || t.Type == WalletTransactionType.CANCEL_FEE)), cancellationToken: cancellationToken);

                if (walletTransactions.Any())
                {
                    string? fcmToken = user.FcmToken;

                    walletTransactions = walletTransactions.OrderBy(t => t.CreatedTime);

                    foreach (WalletTransaction transaction in walletTransactions)
                    {
                        _logger.LogInformation("Begin Resolve pending Transaction: {0}", transaction.Id);

                        if (wallet.Balance >= transaction.Amount)
                        {
                            _logger.LogInformation("\tWallet Balance before: {0}", wallet.Balance);
                            _logger.LogInformation("\tTransaction Amount: {0}", transaction.Amount);
                            
                            wallet.Balance -= transaction.Amount;

                            _logger.LogInformation("\tWallet Balance AFTERWARD: {0}", wallet.Balance);
                            
                            transaction.Status = WalletTransactionStatus.SUCCESSFULL;

                            await work.Wallets.UpdateAsync(wallet, isManuallyAssignTracking: true);
                            await work.WalletTransactions.UpdateAsync(transaction, isManuallyAssignTracking: true);

                            if (transaction.BookingDetailId.HasValue)
                            {
                                BookingDetail bookingDetail = await work.BookingDetails
                                    .GetAsync(transaction.BookingDetailId.Value, cancellationToken: cancellationToken);
                                if (bookingDetail.Status == BookingDetailStatus.PENDING_PAID)
                                {
                                    bookingDetail.Status = BookingDetailStatus.COMPLETED;
                                    await work.BookingDetails.UpdateAsync(bookingDetail, isManuallyAssignTracking: true);
                                }
                            }
                            await work.SaveChangesAsync(cancellationToken);

                            if (fcmToken != null && !string.IsNullOrEmpty(fcmToken))
                            {
                                // Send notification
                                NotificationCreateModel notification = new NotificationCreateModel()
                                {
                                    UserId = userId,
                                    Type = NotificationType.SPECIFIC_USER,
                                    Title = "Thanh toán phí thành công",
                                    Description = "Khoản phí " + transaction.Amount +
                                        " đã được thanh toán thành công!"
                                };
                                Dictionary<string, string> dataToSend = new Dictionary<string, string>()
                            {
                                {"action", NotificationAction.TransactionDetail },
                                    { "walletTransactionId", transaction.Id.ToString() }
                            };

                                await notificationServices.CreateFirebaseNotificationAsync(notification, fcmToken,
                                                        dataToSend, cancellationToken);
                            }
                        }
                        _logger.LogInformation("End Resolve pending Transaction: {0}", transaction.Id);

                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }

        }

        public async Task ScheduleCheckTransactionStatusAsync(
            Guid walletTransactionId, string clientIpAddress,
            IScheduler scheduler,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - SCHEDULE TRANSACTION STATUS CHECKING ======");
            _logger.LogInformation("====== TransactionId: {0} ======", walletTransactionId);
            try
            {
                WalletTransaction walletTransaction = await work.WalletTransactions
                    .GetAsync(walletTransactionId, cancellationToken: cancellationToken);

                if (walletTransaction is null)
                {
                    throw new Exception("Wallet Transaction does not exist! ID: " + walletTransactionId);
                }

                JobKey transactionStatusCheckingJobKey = new JobKey(CronJobIdentities.CHECK_TRANSACTION_STATUS_JOBKEY);


                _logger.LogInformation("Trying to schedule for Transaction: ID: " +
                        walletTransaction.Id + "; Created Time: " 
                        + walletTransaction.CreatedTime.ToString("dd/MM/yyyy - HH:mm:ss"));

                DateTimeOffset scheduleDatetimeOffset = walletTransaction.CreatedTime.ToVnDateTimeOffset()
                    .AddMinutes(5);

                ITrigger trigger = TriggerBuilder.Create()
                    .ForJob(transactionStatusCheckingJobKey)
                    .WithIdentity(CronJobIdentities.CHECK_TRANSACTION_STATUS_TRIGGER_ID + "_" + walletTransaction.Id)
                    .WithDescription("Check for transaction status")
                    .UsingJobData(CronJobIdentities.TRANSACTION_ID_JOB_DATA, walletTransactionId)
                    .UsingJobData(CronJobIdentities.CLIENT_IP_ADDRESS_JOB_DATA, clientIpAddress)
                    .StartAt(scheduleDatetimeOffset)
                    .Build();

                await scheduler.ScheduleJob(trigger);

                _logger.LogInformation("Scheduled for Transaction: ID: " +
                    walletTransaction.Id + "; Schedule Time: " + scheduleDatetimeOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }
    }
}
