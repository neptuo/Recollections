using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Accounts;

public class DbConnectionProvider : IConnectionProvider
{
    private readonly DataContext db;

    public DbConnectionProvider(DataContext db)
    {
        Ensure.NotNull(db, "db");
        this.db = db;
    }

    public async Task<ConnectedUsersModel> GetConnectedUsersForAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new(Array.Empty<string>(), Array.Empty<string>());

        IReadOnlyDictionary<string, ConnectedUsersModel> models = await GetConnectedUsersForAsync([userId]);
        return models.TryGetValue(userId, out ConnectedUsersModel connectedUsers)
            ? connectedUsers
            : new(Array.Empty<string>(), Array.Empty<string>());
    }

    public async Task<IReadOnlyDictionary<string, ConnectedUsersModel>> GetConnectedUsersForAsync(IEnumerable<string> userIds)
    {
        string[] normalizedUserIds = userIds?
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Distinct(System.StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();
        if (normalizedUserIds.Length == 0)
            return new Dictionary<string, ConnectedUsersModel>(System.StringComparer.Ordinal);

        HashSet<string> userIdSet = normalizedUserIds.ToHashSet(System.StringComparer.Ordinal);
        var activeConnections = await db.Connections
            .Where(c => (normalizedUserIds.Contains(c.UserId) || normalizedUserIds.Contains(c.OtherUserId)) && c.State == 2)
            .Select(c => new { c.UserId, c.Permission, c.OtherUserId, c.OtherPermission })
            .ToListAsync();

        Dictionary<string, HashSet<string>> activeUserIdsByUserId = normalizedUserIds.ToDictionary(
            userId => userId,
            _ => new HashSet<string>(System.StringComparer.Ordinal),
            System.StringComparer.Ordinal
        );
        Dictionary<string, HashSet<string>> readerUserIdsByUserId = normalizedUserIds.ToDictionary(
            userId => userId,
            _ => new HashSet<string>(System.StringComparer.Ordinal),
            System.StringComparer.Ordinal
        );

        foreach (var connection in activeConnections)
        {
            if (userIdSet.Contains(connection.UserId))
            {
                activeUserIdsByUserId[connection.UserId].Add(connection.OtherUserId);
                if (connection.OtherPermission == 0 || connection.OtherPermission == 1)
                    readerUserIdsByUserId[connection.UserId].Add(connection.OtherUserId);
            }

            if (userIdSet.Contains(connection.OtherUserId))
            {
                activeUserIdsByUserId[connection.OtherUserId].Add(connection.UserId);
                if (connection.Permission == 0 || connection.Permission == 1)
                    readerUserIdsByUserId[connection.OtherUserId].Add(connection.UserId);
            }
        }

        return normalizedUserIds.ToDictionary(
            userId => userId,
            userId => new ConnectedUsersModel(
                activeUserIdsByUserId[userId].ToList(),
                readerUserIdsByUserId[userId].ToList()
            ),
            System.StringComparer.Ordinal
        );
    }

    public async Task<int?> GetPermissionAsync(string accessingUserId, string ownerUserId)
    {
        return await db.Connections
            .Where(c => ((c.UserId == accessingUserId && c.OtherUserId == ownerUserId) || (c.UserId == ownerUserId && c.OtherUserId == accessingUserId)) && c.State == 2)
            .Select(c => c.UserId == accessingUserId ? c.OtherPermission : c.Permission)
            .SingleOrDefaultAsync();
    }
}
