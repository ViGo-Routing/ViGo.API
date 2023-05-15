using System;
using System.Linq;
using System.Security.Claims;

namespace ViGo.Utilities
{
    public static class IdentityUtilities
    {
        private static ClaimsPrincipal? currentUser;

        public static ClaimsPrincipal CurrentUser
            => currentUser ??
            throw new ApplicationException("The Identity Utilities must be initialized before being used!!!");

        public static void Initialize(ClaimsPrincipal _currentUser)
        {
            currentUser = _currentUser;
        }

        public static Guid GetCurrentUserId()
            => Guid.Parse(
                CurrentUser.Claims.FirstOrDefault(claim =>
                claim.Type.Equals(ClaimTypes.NameIdentifier)).Value);

        public static short GetCurrentRoleId()
            => short.Parse(
                CurrentUser.Claims.FirstOrDefault(claim =>
                claim.Type.Equals(ClaimTypes.Role)).Value);

        public static string GetUserClaim(this ClaimsPrincipal user, string claimType)
        {
            return user.Claims.SingleOrDefault(
                claim => claim.Type.Equals(claimType)).Value;
        }

        public static bool IsAuthenticated(this ClaimsPrincipal user)
            => user == null ? false : user.Identity.IsAuthenticated;
    }
}
