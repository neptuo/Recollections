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
    Task<ConnectedUsersModel> GetConnectedUsersForAsync(string userId);

    /// <summary>
    /// Returns connected users for each provided user in a single batch.
    /// </summary>
    Task<IReadOnlyDictionary<string, ConnectedUsersModel>> GetConnectedUsersForAsync(IEnumerable<string> userIds);

    /// <summary>
    /// Gets permission that <paramref name="targetUserId"/> assigned for <paramref name="currentUserId"/>.
    /// </summary>
    Task<int?> GetPermissionAsync(string currentUserId, string targetUserId);
}

public record ConnectedUsersModel(IReadOnlyList<string> ActiveUserIds, IReadOnlyList<string> ReaderUserIds);
