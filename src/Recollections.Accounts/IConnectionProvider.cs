using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts;

/// <summary>
/// Always returns only active connections
/// </summary>
public interface IConnectionProvider
{
    Task<IReadOnlyList<string>> GetUserIdsWithReaderToAsync(string userId);
    Task<int?> GetPermissionAsync(string currentUserId, string targetUserId);
}