using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts
{
    public static class UserNameProviderExtensions
    {
        public static async Task<string> GetUserNameAsync(this IUserNameProvider provider, string userId)
        {
            Ensure.NotNull(provider, "provider");
            Ensure.NotNullOrEmpty(userId, "userId");
            var userNames = await provider.GetUserNamesAsync(new[] { userId });
            return userNames.First();
        }
    }
}
