using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain;
using ViGo.Models.Users;
using ViGo.Repository.Core;
using ViGo.Services.Core;
using ViGo.Utilities;
using ViGo.Utilities.Configuration;

namespace ViGo.Services
{
    public class FirebaseServices : BaseServices
    {
        public FirebaseServices (IUnitOfWork work) : base(work)
        {
        }

        // Run once, test only
        public async Task<string> CreateFirebaseUserAsync(string phone, CancellationToken cancellationToken)
        {
            //IEnumerable<User> users = await work.Users.GetAllAsync();
            //foreach (User user in users)
            //{
            //    if (!string.IsNullOrEmpty(user.Phone))
            //    {
            //        UserRecordArgs args = new UserRecordArgs()
            //        {
            //            PhoneNumber = user.Phone,
            //            DisplayName = user.Name,
            //            PhotoUrl = string.IsNullOrEmpty(user.AvatarUrl) ? null : user.AvatarUrl,
            //            Disabled = false
            //        };
            //        try
            //        {
            //            UserRecord checkRecord = await FirebaseAuth.DefaultInstance
            //            .GetUserByPhoneNumberAsync(user.Phone);
            //            continue;

            //        }
            //        catch (FirebaseAuthException authException)
            //        {
            //            UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
            //            user.FirebaseUid = userRecord.Uid;
            //            await work.Users.UpdateAsync(user);
            //        }
            //    }

            //}

            //await work.SaveChangesAsync();
            UserRecordArgs recordArgs = new UserRecordArgs
            {
                PhoneNumber = phone,
                Disabled = false
            };
            UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(recordArgs, cancellationToken);
            return userRecord.Uid;
        }

        public async Task<string> GenerateFirebaseToken(string phone, CancellationToken cancellationToken)
        {
            User user = await work.Users.GetAsync(
                u => !string.IsNullOrEmpty(u.Phone) &&
                    u.Phone.ToLower().Trim()
                    .Equals(phone.ToLower().Trim()), cancellationToken: cancellationToken);

            if (user != null)
            {
                string customToken = await FirebaseAuth.DefaultInstance
                    .CreateCustomTokenAsync(user.FirebaseUid, cancellationToken);

                using HttpClient client = new HttpClient();
                string endpoint = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithCustomToken?key=" 
                    + ViGoConfiguration.FirebaseApiKey;

                client.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(
                        "application/json"));

                HttpResponseMessage response = new HttpResponseMessage();

                response = await client.PostAsJsonAsync(endpoint, new FirebaseIdTokenRequest
                {
                    Token = customToken,
                    ReturnSecureToken = true
                }, cancellationToken: cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync(cancellationToken);
                    FirebaseIdTokenResponse? data = 
                        JsonConvert.DeserializeObject<FirebaseIdTokenResponse>(result);

                    if (data != null && !string.IsNullOrEmpty(data.IdToken))
                    {
                        return data.IdToken;
                    }
                }
            }

            return string.Empty;
        }
    }

    internal class FirebaseIdTokenRequest
    {
        public string Token { get; set; }
        public bool ReturnSecureToken { get; set; } = true;
    }

    internal class FirebaseIdTokenResponse
    {
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
    }
}
