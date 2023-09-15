using Google.Cloud.Firestore;
using Newtonsoft.Json;
using ViGo.Utilities.Configuration;

namespace ViGo.Utilities.Google.Firebase
{
    public static class FirestoreUtilities
    {
        private static FirestoreDb? firestoreDb = null;
        private static readonly object dbLock = new object();

        public static FirestoreDb DbInstance
        {
            get
            {
                lock (dbLock)
                {
                    if (firestoreDb is null)
                    {
                        System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "vigo-a7754-firebase-adminsdk-93go8-a26b571de1.json"); firestoreDb = FirestoreDb.Create(ViGoConfiguration.FirebaseProjectId);
                    }
                    return firestoreDb;
                }
            }
        }

        public static async Task<(Guid, string)> SendMessageAsync(this FirestoreDb db,
            Guid bookingDetailId,
            FirestoreMessage message, CancellationToken cancellationToken)
        {
            var collectionRef = db.Collection("vigo-messages")
                .Document(bookingDetailId.ToString().ToLower())
                .Collection("messages");
            Dictionary<string, object> messages = new Dictionary<string, object>()
            {
                {"_id", message.Id.ToString().ToLower() },
                {"createdAt", FieldValue.ServerTimestamp},
                {"sentBy", message.SentBy.ToString().ToLower() },
                {"sentTo", message.SentTo.ToString().ToLower() },
                {"text", message.Text },
                {"user", new Dictionary<string, object> ()
                {
                    {"_id", message.User.Id.ToString().ToLower() },
                    {"avatar", message.User.Avatar },
                    {"name", message.User.Name }
                } }
            };
            await collectionRef.AddAsync(messages, cancellationToken);
            return (message.SentTo, message.Text);
        }
    }

    public class FirestoreMessage
    {
        [JsonProperty("_id")]
        public Guid Id { get; set; }
        //public object CreatedAt { get; set; }
        public Guid SentBy { get; set; }
        public Guid SentTo { get; set; }
        public string Text { get; set; }
        public FirestoreMessageUser User { get; set; }
    }

    public class FirestoreMessageUser
    {
        [JsonProperty("_id")]
        public Guid Id { get; set; }
        public string Avatar { get; set; }
        public string Name { get; set; }
    }
}
