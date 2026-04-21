using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Sharing;

namespace Neptuo.Recollections.Entries;

public class EntryListMapper(DataContext dataContext, IUserNameProvider userNames, ShareStatusService shareStatus, EntryMediaMapper entryMediaMapper)
{
    private const int PageSize = 10;
    private const int MaxPageSize = PageSize * 100;
    private const int QueryBatchSize = 250;
    private const int PreviewMediaCount = 3;
    private IUserNameProvider userNames = userNames;

    public Task<(List<EntryListModel> models, bool hasMore)> MapAsync(IQueryable<Entry> query, string[] userIds, ConnectedUsersModel connectedUsers, int offset, bool includePreviewMedia = false)
        => MapAsync(query, userIds, connectedUsers, offset, PageSize, includePreviewMedia);

    public static int NormalizePageSize(int? pageSize)
    {
        int normalizedPageSize = pageSize ?? PageSize;
        Ensure.Positive(normalizedPageSize, "pageSize");
        return Math.Min(normalizedPageSize, MaxPageSize);
    }

    public async Task<(List<EntryListModel> models, bool hasMore)> MapAsync(IQueryable<Entry> query, string[] userIds, ConnectedUsersModel connectedUsers, int? offset = null, int? pageSize = null, bool includePreviewMedia = false)
    {
        if (offset != null)
        {
            Ensure.PositiveOrZero(offset.Value, "offset");
            query = query.Skip(offset.Value);
        }

        int? normalizedPageSize = null;
        if (pageSize != null)
        {
            normalizedPageSize = NormalizePageSize(pageSize.Value);
            query = query.Take(normalizedPageSize.Value);
        }

        var result = await query
            .Select(e => new
            {
                UserId = e.UserId,
                Id = e.Id,
                Title = e.Title,
                When = e.When,
                StoryTitle = e.Story.Title ?? e.Chapter.Story.Title,
                ChapterTitle = e.Chapter.Title,
                Beings = new List<EntryBeingModel>(),
                ImageCount = dataContext.Images.Count(i => i.Entry.Id == e.Id),
                VideoCount = dataContext.Videos.Count(v => v.Entry.Id == e.Id),
                GpsCount = e.Locations.Count,
                BeingCount = e.Beings.Count(),
                Text = e.Text
            })
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        Dictionary<string, List<EntryBeingModel>> beingsByEntryId = [];
        List<string> entryIdsWithBeings = result
            .Where(e => e.BeingCount > 0)
            .Select(e => e.Id)
            .ToList();

        if (entryIdsWithBeings.Count > 0)
        {
            foreach (List<string> entryIdBatch in Batch(entryIdsWithBeings, QueryBatchSize))
            {
                // Single query: load entry↔being links with being data, filtered by accessibility
                var accessibleEntryBeings = await shareStatus
                    .OwnedByOrExplicitlySharedWithUser(
                        dataContext,
                        dataContext.Beings
                            .AsNoTracking()
                            .Where(b => b.Entries.Any(e => entryIdBatch.Contains(e.Id))),
                        userIds,
                        connectedUsers)
                    .SelectMany(
                        b => b.Entries.Where(e => entryIdBatch.Contains(e.Id)),
                        (b, e) => new
                        {
                            EntryId = e.Id,
                            BeingId = b.Id,
                            b.Name,
                            b.Icon
                        })
                    .ToListAsync();

                foreach (var item in accessibleEntryBeings
                    .OrderBy(item => item.EntryId)
                    .ThenBy(item => item.Name))
                {
                    if (!beingsByEntryId.TryGetValue(item.EntryId, out List<EntryBeingModel> entryBeings))
                        beingsByEntryId[item.EntryId] = entryBeings = [];

                    entryBeings.Add(new EntryBeingModel(
                        Id: item.BeingId,
                        Name: item.Name,
                        Icon: item.Icon
                    ));
                }
            }
        }

        Dictionary<string, List<MediaModel>> previewMediaByEntryId = [];
        if (includePreviewMedia)
        {
            Dictionary<string, string> entryIdsWithMedia = result
                .Where(e => e.ImageCount > 0 || e.VideoCount > 0)
                .ToDictionary(e => e.Id, e => e.UserId);

            if (entryIdsWithMedia.Count > 0)
            {
                previewMediaByEntryId = await entryMediaMapper.MapByEntryIdAsync(
                    entryIdsWithMedia,
                    PreviewMediaCount
                );
            }
        }

        string[] allUserIds = result.Select(e => e.UserId).ToArray();
        string[] distinctUserIds = allUserIds.Distinct().ToArray();
        var distinctUserNames = await this.userNames.GetUserNamesAsync(distinctUserIds);
        var userNameByUserId = new Dictionary<string, string>(distinctUserIds.Length);
        for (int i = 0; i < distinctUserIds.Length; i++)
            userNameByUserId[distinctUserIds[i]] = distinctUserNames[i];

        return (result.Select((e, index) => new EntryListModel(
            UserId: e.UserId,
            UserName: userNameByUserId[e.UserId],
            Id: e.Id,
            Title: e.Title,
            TextWordCount: (e.Text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
            When: e.When,
            StoryTitle: e.StoryTitle,
            ChapterTitle: e.ChapterTitle,
            Beings: beingsByEntryId.TryGetValue(e.Id, out List<EntryBeingModel> beings)
                ? beings
                : e.Beings,
            ImageCount: e.ImageCount,
            VideoCount: e.VideoCount,
            GpsCount: e.GpsCount
        )
        {
            PreviewMedia = previewMediaByEntryId.TryGetValue(e.Id, out List<MediaModel> media)
                ? media
                : []
        }).ToList(), normalizedPageSize != null && result.Count == normalizedPageSize.Value);
    }

    private static IEnumerable<List<string>> Batch(List<string> values, int size)
    {
        for (int i = 0; i < values.Count; i += size)
            yield return values.Skip(i).Take(size).ToList();
    }
}
