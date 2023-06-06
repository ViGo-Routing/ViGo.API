using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Utilities
{
    public static class FirebaseUtilities
    {
        public static async Task<string> SendNotificationToDeviceAsync(string fcmToken, 
            string title, string content, string? imageUrl)
        {
            Notification notification = new Notification
            {
                Title = title,
                Body = content,
                ImageUrl = imageUrl,
            };
            Message message = new Message
            {
                Notification = notification,
                Token = fcmToken,
            };

            string response = await FirebaseMessaging.DefaultInstance
                .SendAsync(message);

            return response;
        }

        public static async Task<IEnumerable<string>> SendNotificationToDevicesAsync
            (IList<string> fcmTokens,
            string title, string content, string? imageUrl)
        {
            Notification notification = new Notification
            {
                Title = title,
                Body = content,
                ImageUrl = imageUrl
            };
            MulticastMessage message = new MulticastMessage
            {
                Notification = notification,
                Tokens = fcmTokens.ToList()
            };

            var response = await FirebaseMessaging.DefaultInstance
                .SendMulticastAsync(message);

            IEnumerable<string> failures = new List<string>();
            if (response.FailureCount > 0)
            {
                int responseCount = response.Responses.Count;
                for (int i = 0; i < responseCount; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        failures = failures.Append(fcmTokens[i]);
                    }
                }
            }

            return failures;
        }

        public static async Task<string> SendNotificationToTopicAsync(
            string topic, string title, string content, string? imageUrl)
        {
            Notification notification = new Notification
            {
                Title = title,
                Body = content,
                ImageUrl = imageUrl
            };

            Message message = new Message
            {
                Notification = notification,
                Topic = topic
            };

            string response = await FirebaseMessaging.DefaultInstance
                .SendAsync(message);

            return response;
        }
    }
}
