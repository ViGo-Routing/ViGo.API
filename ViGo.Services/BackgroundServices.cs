using Microsoft.Extensions.Logging;
using ViGo.Domain;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities.Extensions;
using ViGo.Utilities.Google.Firebase;

namespace ViGo.Services
{
    public partial class BackgroundServices : UseNotificationServices
    {
        public BackgroundServices(IUnitOfWork work, ILogger logger) : base(work, logger)
        {
        }

        public async Task SendMessageNotificationAsync(
            Guid receiver, string text, CancellationToken cancellationToken)
        {
            _logger.LogInformation("====== BEGIN TASK - SEND NOTIFICATION - NEW MESSAGE RECEIVED ======");
            _logger.LogInformation("====== UserId: {0} ======", receiver);

            try
            {
                User? user = await work.Users.GetAsync(receiver, cancellationToken: cancellationToken);

                if (user is null)
                {
                    throw new Exception("Receiver does not exist!! ReceiverId: " + receiver);
                }

                string? fcmToken = user.FcmToken;
                if (!string.IsNullOrEmpty(fcmToken))
                {
                    string firebaseResult = await FirebaseUtilities.SendNotificationToDeviceAsync(fcmToken,
                        "Bạn có 1 tin nhắn mới", text, data: null, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error has occured: {0}", ex.GeneratorErrorMessage());
            }
        }
    }
}
