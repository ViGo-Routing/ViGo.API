using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Domain.Enumerations;
using ViGo.Models.Notifications;
using ViGo.Utilities.Extensions;

namespace ViGo.Services
{
    public partial class BackgroundServices
    {
        public async Task CheckForPendingTransactions(Guid userId,
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

                            await work.Wallets.UpdateAsync(wallet);
                            await work.WalletTransactions.UpdateAsync(transaction);
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
    }
}
