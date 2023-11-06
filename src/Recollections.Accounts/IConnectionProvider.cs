using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts;

/// <summary>
/// Always returns only active connections.
/// </summary>
public interface IConnectionProvider
{
    /// <summary>
    /// Returns connected user ids which assigned at least reader permission to <paramref name="userId"/>.
    /// </summary>
    Task<IReadOnlyList<string>> GetUserIdsWithReaderToAsync(string userId);

    /// <summary>
    /// Gets permission that <paramref name="targetUserId"/> assigned for <paramref name="currentUserId"/>.
    /// </summary>
    Task<int?> GetPermissionAsync(string currentUserId, string targetUserId);
}