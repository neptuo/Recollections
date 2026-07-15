using Neptuo.Recollections.Components;
using System.Collections.Generic;

namespace Neptuo.Recollections.Entries;

internal static class GalleryModelMapper
{
    public static void AddMapped(ICollection<GalleryModel> target, IEnumerable<MediaModel> media, Api api)
    {
        Ensure.NotNull(target, nameof(target));
        Ensure.NotNull(media, nameof(media));
        Ensure.NotNull(api, nameof(api));

        foreach (MediaModel item in media)
        {
            if (TryMap(item, api, out GalleryModel model))
                target.Add(model);
        }
    }

    public static bool TryMap(MediaModel item, Api api, out GalleryModel model)
    {
        Ensure.NotNull(api, nameof(api));

        if (item?.Image != null)
        {
            model = new GalleryModel
            {
                Type = "image",
                Title = item.Image.Name,
                Width = item.Image.Preview.Width,
                Height = item.Image.Preview.Height,
                PreviewUrl = api.GetMediaUrl(item.Image.Preview.Url),
            };
            return true;
        }

        if (item?.Video != null)
        {
            model = new GalleryModel
            {
                Type = "video",
                Title = item.Video.Name,
                SizeText = Utils.FileSizeText(item.Video.OriginalSize),
                Width = item.Video.Preview.Width,
                Height = item.Video.Preview.Height,
                ContentType = item.Video.ContentType,
                PreviewUrl = api.GetMediaUrl(item.Video.Preview.Url),
                OriginalUrl = api.GetMediaUrl(item.Video.Original.Url),
            };
            return true;
        }

        model = null;
        return false;
    }
}
