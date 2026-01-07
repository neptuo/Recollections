namespace Neptuo.Recollections.Entries;

public interface IMediaUrlList
{
    MediaSourceModel Thumbnail { get; }
    MediaSourceModel Preview { get; }
    MediaSourceModel Original { get; }
}