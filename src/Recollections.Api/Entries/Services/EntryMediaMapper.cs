using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries;

public class EntryMediaMapper(DataContext dataContext, ImageService imageService, VideoService videoService)
{
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
        IQueryable<Image> imageQuery = CreateImageQuery(entryIds, takePerEntry);
        IQueryable<Video> videoQuery = CreateVideoQuery(entryIds, takePerEntry);

        var images = await imageQuery
            .Select(i => new
            {
                EntryId = i.Entry.Id,
                Entity = i
            })
            .AsNoTracking()
            .ToListAsync();

        var videos = await videoQuery
            .Select(v => new
            {
                EntryId = v.Entry.Id,
                Entity = v
            })
            .AsNoTracking()
            .ToListAsync();

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

    private IQueryable<Image> CreateImageQuery(List<string> entryIds, int? takePerEntry)
    {
        if (takePerEntry == null)
        {
            return dataContext.Images
                .Where(i => entryIds.Contains(i.Entry.Id));
        }

        IQueryable<Image> result = dataContext.Images.Where(i => false);
        foreach (string entryId in entryIds)
        {
            string currentEntryId = entryId;
            result = result.Concat(
                dataContext.Images
                    .Where(i => i.Entry.Id == currentEntryId)
                    .OrderBy(i => i.When)
                    .Take(takePerEntry.Value)
            );
        }

        return result;
    }

    private IQueryable<Video> CreateVideoQuery(List<string> entryIds, int? takePerEntry)
    {
        if (takePerEntry == null)
        {
            return dataContext.Videos
                .Where(v => entryIds.Contains(v.Entry.Id));
        }

        IQueryable<Video> result = dataContext.Videos.Where(v => false);
        foreach (string entryId in entryIds)
        {
            string currentEntryId = entryId;
            result = result.Concat(
                dataContext.Videos
                    .Where(v => v.Entry.Id == currentEntryId)
                    .OrderBy(v => v.When)
                    .Take(takePerEntry.Value)
            );
        }

        return result;
    }
}
