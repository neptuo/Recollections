using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections
{
    public static class ClaimsPrincipalExtensions
    {
        private const string ReadOnly = "readonly";

        public static string FindUserId(this IEnumerable<Claim> user)
        {
            Ensure.NotNull(user, "user");

            string userId = user.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (String.IsNullOrEmpty(userId))
                return null;

            return userId;
        }

        public static string FindUserId(this ClaimsPrincipal user)
        {
            Ensure.NotNull(user, "user");

            string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (String.IsNullOrEmpty(userId))
                return null;

            return userId;
        }

        public static string FindUserName(this ClaimsPrincipal user)
        {
            Ensure.NotNull(user, "user");

            string userName = user.FindFirst(ClaimTypes.Name)?.Value;
            if (String.IsNullOrEmpty(userName))
                return null;

            return userName;
        }

        public static bool IsReadOnly(this IEnumerable<Claim> user)
        {
            Ensure.NotNull(user, "user");

            string isReadonly = user.FirstOrDefault(c => c.Type == ReadOnly)?.Value;
            return IsReadOnly(isReadonly);
        }

        public static bool IsReadOnly(this ClaimsPrincipal user)
        {
            Ensure.NotNull(user, "user");

            string isReadonly = user.FindFirst(ReadOnly)?.Value;
            return IsReadOnly(isReadonly);
        }

        private static bool IsReadOnly(string isReadonly)
        {
            if (String.IsNullOrEmpty(isReadonly))
                return false;

            if (Boolean.TryParse(isReadonly, out var value) && value)
                return true;

            return false;
        }

        public static void IsReadOnly(this List<Claim> claims, bool isReadOnly)
        {
            if (isReadOnly)
                claims.Add(new Claim(ReadOnly, Boolean.TrueString));
        }
    }
}
