using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries;

public class EntryMediaMapper(DataContext dataContext, ImageService imageService, VideoService videoService)
{
    // Keep IN (...) lists well below SQLite's default 999-parameter limit and SQL Server's 2100 limit.
    private const int QueryBatchSize = 250;

    public async Task<List<MediaModel>> MapAsync(string entryId, string userId, int? take = null)
    {
        Ensure.NotNullOrEmpty(entryId, nameof(entryId));
        Ensure.NotNullOrEmpty(userId, nameof(userId));

        var mediaByEntryId = await MapByEntryIdAsync(new Dictionary<string, string>
        {
            [entryId] = userId
        }, take);

        return mediaByEntryId.TryGetValue(entryId, out List<MediaModel> media)
            ? media
            : [];
    }

    public async Task<Dictionary<string, List<MediaModel>>> MapByEntryIdAsync(IReadOnlyDictionary<string, string> userIdsByEntryId, int? takePerEntry = null)
    {
        Ensure.NotNull(userIdsByEntryId, nameof(userIdsByEntryId));
        if (userIdsByEntryId.Count == 0)
            return [];

        List<string> entryIds = userIdsByEntryId.Keys.ToList();

        // Load all images and videos for the matching entries in a single query each
        List<(string EntryId, Image Entity)> images = await LoadImagesAsync(entryIds);
        List<(string EntryId, Video Entity)> videos = await LoadVideosAsync(entryIds);

        var result = new Dictionary<string, List<MediaModel>>(entryIds.Count);
        foreach (string entryId in entryIds)
            result[entryId] = [];

        foreach (var image in images)
        {
            if (!result.TryGetValue(image.EntryId, out List<MediaModel> media))
                continue;

            var model = new ImageModel();
            imageService.MapEntityToModel(image.Entity, model, userIdsByEntryId[image.EntryId], image.EntryId);
            media.Add(new MediaModel { Type = "image", Image = model });
        }

        foreach (var video in videos)
        {
            if (!result.TryGetValue(video.EntryId, out List<MediaModel> media))
                continue;

            var model = new VideoModel();
            videoService.MapEntityToModel(video.Entity, model, userIdsByEntryId[video.EntryId], video.EntryId);
            media.Add(new MediaModel { Type = "video", Video = model });
        }

        // Order by timestamp and trim per entry in memory
        foreach (string entryId in entryIds)
        {
            List<MediaModel> ordered = result[entryId]
                .OrderBy(GetWhen)
                .ToList();

            if (takePerEntry != null)
                ordered = ordered.Take(takePerEntry.Value).ToList();

            result[entryId] = ordered;
        }

        return result;
    }

    private static DateTime GetWhen(MediaModel media)
        => media.Image?.When ?? media.Video?.When ?? DateTime.MinValue;

    private async Task<List<(string EntryId, Image Entity)>> LoadImagesAsync(List<string> entryIds)
    {
        var result = new List<(string EntryId, Image Entity)>();
        foreach (List<string> batch in Batch(entryIds, QueryBatchSize))
        {
            var images = await dataContext.Images
                .Where(i => batch.Contains(i.Entry.Id))
                .Select(i => new
                {
                    EntryId = i.Entry.Id,
                    Entity = i
                })
                .AsNoTracking()
                .ToListAsync();

            result.AddRange(images.Select(i => (i.EntryId, i.Entity)));
        }

        return result;
    }

    private async Task<List<(string EntryId, Video Entity)>> LoadVideosAsync(List<string> entryIds)
    {
        var result = new List<(string EntryId, Video Entity)>();
        foreach (List<string> batch in Batch(entryIds, QueryBatchSize))
        {
            var videos = await dataContext.Videos
                .Where(v => batch.Contains(v.Entry.Id))
                .Select(v => new
                {
                    EntryId = v.Entry.Id,
                    Entity = v
                })
                .AsNoTracking()
                .ToListAsync();

            result.AddRange(videos.Select(v => (v.EntryId, v.Entity)));
        }

        return result;
    }

    private static IEnumerable<List<string>> Batch(List<string> values, int size)
    {
        if (values.Count <= size)
        {
            yield return values;
            yield break;
        }

        for (int i = 0; i < values.Count; i += size)
            yield return values.Skip(i).Take(size).ToList();
    }
}
