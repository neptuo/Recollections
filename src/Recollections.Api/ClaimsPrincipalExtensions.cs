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

        public static string FindUserId(this ClaimsPrincipal user)
        {
            string userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (String.IsNullOrEmpty(userId))
                return null;

            return userId;
        }

        public static bool IsReadOnly(this ClaimsPrincipal user)
        {
            Ensure.NotNull(user, "user");
            string isReadonly = user.FindFirst(ReadOnly)?.Value;
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
