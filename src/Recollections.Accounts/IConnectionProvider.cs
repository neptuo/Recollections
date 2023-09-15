using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts;

public interface IConnectionProvider
{
    Task<IReadOnlyList<string>> GetUserIdsWithReaderToAsync(string userId);
    Task<int?> GetPermissionAsync(string currentUserId, string targetUserId);
}