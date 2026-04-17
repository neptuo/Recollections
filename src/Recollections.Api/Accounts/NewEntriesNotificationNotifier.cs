using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neptuo;
using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Sharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountsDataContext = Neptuo.Recollections.Accounts.DataContext;
using EntriesDataContext = Neptuo.Recollections.Entries.DataContext;

namespace Neptuo.Recollections.Accounts.Notifications
{
    public sealed class NewEntriesNotificationSnapshot
    {
        private readonly Dictionary<string, HashSet<string>> entryIdsByUserId;

        public static NewEntriesNotificationSnapshot Empty { get; } = new(new Dictionary<string, HashSet<string>>(StringComparer.Ordinal));

        internal IEnumerable<string> UserIds => entryIdsByUserId.Keys;

        internal NewEntriesNotificationSnapshot(Dictionary<string, HashSet<string>> entryIdsByUserId)
        {
            this.entryIdsByUserId = entryIdsByUserId;
        }

        internal bool Contains(string userId, string entryId)
            => entryIdsByUserId.TryGetValue(userId, out HashSet<string> entryIds) && entryIds.Contains(entryId);

        internal IReadOnlyCollection<string> GetEntryIds(string userId)
            => entryIdsByUserId.TryGetValue(userId, out HashSet<string> entryIds)
                ? entryIds
                : Array.Empty<string>();
    }

    public class NewEntriesNotificationNotifier
    {
        private static readonly ConnectedUsersModel EmptyConnectedUsers = new(Array.Empty<string>(), Array.Empty<string>());

        private readonly AccountsDataContext accountsDb;
        private readonly EntriesDataContext entriesDb;
        private readonly IConnectionProvider connections;
        private readonly ShareStatusService shareStatus;
        private readonly PushNotificationSender sender;
        private readonly ILogger<NewEntriesNotificationNotifier> log;

        public NewEntriesNotificationNotifier(AccountsDataContext accountsDb, EntriesDataContext entriesDb, IConnectionProvider connections, ShareStatusService shareStatus, PushNotificationSender sender, ILogger<NewEntriesNotificationNotifier> log)
        {
            Ensure.NotNull(accountsDb, "accountsDb");
            Ensure.NotNull(entriesDb, "entriesDb");
            Ensure.NotNull(connections, "connections");
            Ensure.NotNull(shareStatus, "shareStatus");
            Ensure.NotNull(sender, "sender");
            Ensure.NotNull(log, "log");
            this.accountsDb = accountsDb;
            this.entriesDb = entriesDb;
            this.connections = connections;
            this.shareStatus = shareStatus;
            this.sender = sender;
            this.log = log;
        }

        public Task<NewEntriesNotificationSnapshot> CaptureEntriesAsync(params string[] entryIds)
            => CaptureEntriesAsync((IEnumerable<string>)entryIds);

        public async Task<NewEntriesNotificationSnapshot> CaptureEntriesAsync(IEnumerable<string> entryIds)
        {
            List<string> normalizedEntryIds = NormalizeEntryIds(entryIds);
            if (normalizedEntryIds.Count == 0)
                return NewEntriesNotificationSnapshot.Empty;

            List<string> candidateUserIds = await LoadCandidateUserIdsAsync(normalizedEntryIds);
            if (candidateUserIds.Count == 0)
                return NewEntriesNotificationSnapshot.Empty;

            IReadOnlyDictionary<string, ConnectedUsersModel> connectedUsersByUserId = await connections.GetConnectedUsersForAsync(candidateUserIds);
            Dictionary<string, HashSet<string>> result = new(StringComparer.Ordinal);
            IQueryable<Entry> candidateEntries = entriesDb.Entries
                .AsNoTracking()
                .Where(e => normalizedEntryIds.Contains(e.Id));

            foreach (string userId in candidateUserIds)
            {
                ConnectedUsersModel connectedUsers = connectedUsersByUserId.TryGetValue(userId, out ConnectedUsersModel candidateConnections)
                    ? candidateConnections
                    : EmptyConnectedUsers;
                List<string> visibleEntryIds = await shareStatus
                    .OwnedByOrExplicitlySharedWithUser(entriesDb, candidateEntries, userId, connectedUsers)
                    .Where(e => e.UserId != userId)
                    .Select(e => e.Id)
                    .Distinct()
                    .ToListAsync();

                if (visibleEntryIds.Count > 0)
                    result[userId] = visibleEntryIds.ToHashSet(StringComparer.Ordinal);
            }

            return new NewEntriesNotificationSnapshot(result);
        }

        public async Task<NewEntriesNotificationSnapshot> CaptureStoryAsync(string storyId)
            => await CaptureEntriesAsync(await FindStoryEntryIdsAsync(storyId));

        public async Task<NewEntriesNotificationSnapshot> CaptureBeingAsync(string beingId)
            => await CaptureEntriesAsync(await FindBeingEntryIdsAsync(beingId));

        public async Task NotifyEntriesAsync(IEnumerable<string> entryIds, NewEntriesNotificationSnapshot beforeSnapshot, string trigger)
        {
            List<string> normalizedEntryIds = NormalizeEntryIds(entryIds);
            if (normalizedEntryIds.Count == 0)
                return;

            if (!sender.IsConfigured)
            {
                log.LogDebug("Skipping immediate new entry notifications after '{Trigger}' because push delivery is not configured.", trigger);
                return;
            }

            beforeSnapshot ??= NewEntriesNotificationSnapshot.Empty;
            NewEntriesNotificationSnapshot afterSnapshot = await CaptureEntriesAsync(normalizedEntryIds);

            Dictionary<string, List<string>> newEntryIdsByUserId = new(StringComparer.Ordinal);
            foreach (string userId in afterSnapshot.UserIds)
            {
                List<string> newEntryIds = afterSnapshot
                    .GetEntryIds(userId)
                    .Where(entryId => !beforeSnapshot.Contains(userId, entryId))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (newEntryIds.Count > 0)
                    newEntryIdsByUserId[userId] = newEntryIds;
            }

            if (newEntryIdsByUserId.Count == 0)
            {
                log.LogDebug("No newly available entries to notify after '{Trigger}'.", trigger);
                return;
            }

            List<string> targetUserIds = newEntryIdsByUserId.Keys.ToList();
            Dictionary<string, PushNotificationSender.NewEntryNotificationItem> notificationEntriesById = (await entriesDb.Entries
                .AsNoTracking()
                .Where(e => normalizedEntryIds.Contains(e.Id))
                .Select(e => new PushNotificationSender.NewEntryNotificationItem(e.Id, e.Title))
                .ToListAsync())
                .ToDictionary(e => e.Id, e => e, StringComparer.Ordinal);

            List<UserNotificationPushSubscription> activeSubscriptions = await accountsDb.PushSubscriptions
                .Where(s => s.RevokedAt == null && targetUserIds.Contains(s.UserId))
                .ToListAsync();
            Dictionary<string, List<UserNotificationPushSubscription>> subscriptionsByUserId = activeSubscriptions
                .GroupBy(s => s.UserId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            foreach (var pair in newEntryIdsByUserId)
            {
                if (!subscriptionsByUserId.TryGetValue(pair.Key, out List<UserNotificationPushSubscription> userSubscriptions) || userSubscriptions.Count == 0)
                {
                    log.LogDebug("Skipping immediate new entry notifications for user '{UserId}' after '{Trigger}' because there are no active subscriptions.", pair.Key, trigger);
                    continue;
                }

                List<UserNotificationNewEntriesDispatch> reservedDispatches = await ReserveDispatchesAsync(pair.Key, pair.Value);
                if (reservedDispatches.Count == 0)
                {
                    log.LogDebug("Skipping immediate new entry notifications for user '{UserId}' after '{Trigger}' because all {EntryCount} entries were already notified.", pair.Key, trigger, pair.Value.Count);
                    continue;
                }

                log.LogDebug("Sending immediate new entry notification to user '{UserId}' for {EntryCount} newly available entries after '{Trigger}'.", pair.Key, reservedDispatches.Count, trigger);
                List<PushNotificationSender.NewEntryNotificationItem> notificationEntries = reservedDispatches
                    .Select(d => notificationEntriesById.TryGetValue(d.EntryId, out PushNotificationSender.NewEntryNotificationItem entry)
                        ? entry
                        : new PushNotificationSender.NewEntryNotificationItem(d.EntryId, null))
                    .ToList();

                int deliveredCount = await sender.SendNewEntriesAsync(userSubscriptions, notificationEntries);
                if (deliveredCount < 1)
                {
                    accountsDb.NotificationNewEntriesDispatches.RemoveRange(reservedDispatches);
                    log.LogWarning("No immediate new entry notifications were delivered to user '{UserId}' after '{Trigger}'. Releasing {EntryCount} reserved dispatch row(s).", pair.Key, trigger, reservedDispatches.Count);
                }
                else
                {
                    DateTime sentAt = DateTime.Now;
                    foreach (UserNotificationNewEntriesDispatch dispatch in reservedDispatches)
                        dispatch.SentAt = sentAt;
                }

                if (accountsDb.ChangeTracker.HasChanges())
                    await accountsDb.SaveChangesAsync();
            }
        }

        public async Task NotifyStoryAsync(string storyId, NewEntriesNotificationSnapshot beforeSnapshot, string trigger)
            => await NotifyEntriesAsync(await FindStoryEntryIdsAsync(storyId), beforeSnapshot, trigger);

        public async Task NotifyBeingAsync(string beingId, NewEntriesNotificationSnapshot beforeSnapshot, string trigger)
            => await NotifyEntriesAsync(await FindBeingEntryIdsAsync(beingId), beforeSnapshot, trigger);

        private async Task<List<string>> LoadCandidateUserIdsAsync(IReadOnlyCollection<string> entryIds)
        {
            List<string> enabledUserIds = await accountsDb.NotificationSettings
                .Where(s => s.IsEnabled)
                .Join(
                    accountsDb.NotificationNewEntriesSettings.Where(s => s.IsEnabled),
                    settings => settings.UserId,
                    topic => topic.UserId,
                    (settings, topic) => settings.UserId
                )
                .Join(
                    accountsDb.PushSubscriptions.Where(s => s.RevokedAt == null),
                    userId => userId,
                    subscription => subscription.UserId,
                    (userId, subscription) => userId
                )
                .Distinct()
                .ToListAsync();

            if (enabledUserIds.Count == 0)
                return enabledUserIds;

            HashSet<string> relevantUserIds = await LoadRelevantUserIdsAsync(entryIds);
            if (relevantUserIds.Count == 0)
                return new List<string>();

            return enabledUserIds
                .Where(relevantUserIds.Contains)
                .ToList();
        }

        private async Task<List<UserNotificationNewEntriesDispatch>> ReserveDispatchesAsync(string userId, IEnumerable<string> entryIds)
        {
            List<string> normalizedEntryIds = NormalizeEntryIds(entryIds);
            if (normalizedEntryIds.Count == 0)
                return new List<UserNotificationNewEntriesDispatch>();

            HashSet<string> reservedEntryIds = await FindReservedEntryIdsAsync(userId, normalizedEntryIds);
            DateTime created = DateTime.Now;
            List<UserNotificationNewEntriesDispatch> result = CreateDispatches(
                userId,
                normalizedEntryIds.Where(entryId => !reservedEntryIds.Contains(entryId)),
                created
            );

            if (result.Count == 0)
                return result;

            if (await TrySaveDispatchesAsync(result))
                return result;

            reservedEntryIds = await FindReservedEntryIdsAsync(userId, normalizedEntryIds);
            result = CreateDispatches(
                userId,
                normalizedEntryIds.Where(entryId => !reservedEntryIds.Contains(entryId)),
                created
            );

            if (result.Count == 0)
                return result;

            return await TrySaveDispatchesAsync(result)
                ? result
                : await ReserveDispatchesOneByOneAsync(userId, normalizedEntryIds, created);
        }

        private async Task<List<string>> FindStoryEntryIdsAsync(string storyId)
        {
            if (String.IsNullOrWhiteSpace(storyId))
                return new List<string>();

            return await entriesDb.Entries
                .AsNoTracking()
                .Where(e =>
                    (e.Story != null && e.Story.Id == storyId)
                    || (e.Chapter != null && e.Chapter.Story.Id == storyId)
                )
                .Select(e => e.Id)
                .Distinct()
                .ToListAsync();
        }

        private async Task<List<string>> FindBeingEntryIdsAsync(string beingId)
        {
            if (String.IsNullOrWhiteSpace(beingId))
                return new List<string>();

            return await entriesDb.Entries
                .AsNoTracking()
                .Where(e => e.Beings.Any(b => b.Id == beingId))
                .Select(e => e.Id)
                .Distinct()
                .ToListAsync();
        }

        private static List<string> NormalizeEntryIds(IEnumerable<string> entryIds)
            => entryIds?
                .Where(id => !String.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList()
                ?? new List<string>();

        private async Task<HashSet<string>> LoadRelevantUserIdsAsync(IReadOnlyCollection<string> entryIds)
        {
            HashSet<string> relevantUserIds = new(StringComparer.Ordinal);
            List<string> ownerUserIds = await entriesDb.Entries
                .AsNoTracking()
                .Where(e => entryIds.Contains(e.Id))
                .Select(e => e.UserId)
                .Distinct()
                .ToListAsync();

            relevantUserIds.UnionWith(await entriesDb.EntryShares
                .AsNoTracking()
                .Where(s => entryIds.Contains(s.EntryId))
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync());

            List<string> storyIds = await entriesDb.Entries
                .AsNoTracking()
                .Where(e => entryIds.Contains(e.Id) && (e.Story != null || e.Chapter != null))
                .Select(e => e.Story != null ? e.Story.Id : e.Chapter.Story.Id)
                .Distinct()
                .ToListAsync();
            if (storyIds.Count > 0)
            {
                relevantUserIds.UnionWith(await entriesDb.StoryShares
                    .AsNoTracking()
                    .Where(s => storyIds.Contains(s.StoryId))
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync());
            }

            relevantUserIds.UnionWith(await entriesDb.Entries
                .AsNoTracking()
                .Where(e => entryIds.Contains(e.Id))
                .SelectMany(e => e.Beings)
                .Select(b => b.UserId)
                .Distinct()
                .ToListAsync());

            List<string> beingIds = await entriesDb.Entries
                .AsNoTracking()
                .Where(e => entryIds.Contains(e.Id))
                .SelectMany(e => e.Beings.Where(b => b.Id != b.UserId && !b.IsSharingInherited))
                .Select(b => b.Id)
                .Distinct()
                .ToListAsync();
            if (beingIds.Count > 0)
            {
                relevantUserIds.UnionWith(await entriesDb.BeingShares
                    .AsNoTracking()
                    .Where(s => beingIds.Contains(s.BeingId))
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync());
            }

            IReadOnlyDictionary<string, ConnectedUsersModel> connectedUsersByOwnerId = await connections.GetConnectedUsersForAsync(ownerUserIds);
            foreach (ConnectedUsersModel connectedUsers in connectedUsersByOwnerId.Values)
                relevantUserIds.UnionWith(connectedUsers.ReaderUserIds);

            relevantUserIds.RemoveWhere(String.IsNullOrWhiteSpace);
            return relevantUserIds;
        }

        private Task<HashSet<string>> FindReservedEntryIdsAsync(string userId, IReadOnlyCollection<string> entryIds)
        {
            return accountsDb.NotificationNewEntriesDispatches
                .AsNoTracking()
                .Where(d => d.UserId == userId && entryIds.Contains(d.EntryId))
                .Select(d => d.EntryId)
                .ToHashSetAsync(StringComparer.Ordinal);
        }

        private static List<UserNotificationNewEntriesDispatch> CreateDispatches(string userId, IEnumerable<string> entryIds, DateTime created)
        {
            return entryIds
                .Select(entryId => new UserNotificationNewEntriesDispatch()
                {
                    UserId = userId,
                    EntryId = entryId,
                    Created = created
                })
                .ToList();
        }

        private async Task<bool> TrySaveDispatchesAsync(List<UserNotificationNewEntriesDispatch> dispatches)
        {
            accountsDb.NotificationNewEntriesDispatches.AddRange(dispatches);
            try
            {
                await accountsDb.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                foreach (UserNotificationNewEntriesDispatch dispatch in dispatches)
                    accountsDb.Entry(dispatch).State = EntityState.Detached;

                return false;
            }
        }

        private async Task<List<UserNotificationNewEntriesDispatch>> ReserveDispatchesOneByOneAsync(string userId, IReadOnlyCollection<string> entryIds, DateTime created)
        {
            List<UserNotificationNewEntriesDispatch> result = new();
            HashSet<string> reservedEntryIds = await FindReservedEntryIdsAsync(userId, entryIds);
            foreach (UserNotificationNewEntriesDispatch dispatch in CreateDispatches(userId, entryIds.Where(entryId => !reservedEntryIds.Contains(entryId)), created))
            {
                if (await TrySaveDispatchesAsync(new List<UserNotificationNewEntriesDispatch> { dispatch }))
                    result.Add(dispatch);
            }

            return result;
        }
    }
}
