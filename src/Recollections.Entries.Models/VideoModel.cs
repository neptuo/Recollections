using System;

namespace Neptuo.Recollections.Entries
{
    public class VideoModel : ICloneable<VideoModel>, IEquatable<VideoModel>, IMediaUrlList
    {
        public string Id { get; set; }
        public string UserId { get; set; }

        public MediaSourceModel Thumbnail { get; set; }
        public MediaSourceModel Preview { get; set; }
        public MediaSourceModel Original { get; set; }

        public string ContentType { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime When { get; set; }
        
        public LocationModel Location { get; set; } = new();
        public double? Duration { get; set; }

        public VideoModel Clone() => new VideoModel()
        {
            Id = Id,
            UserId = UserId,
            Thumbnail = Thumbnail.Clone(),
            Preview = Preview.Clone(),
            Original = Original.Clone(),
            ContentType = ContentType,
            Name = Name,
            Description = Description,
            When = When,
            Location = Location.Clone(),
            Duration = Duration
        };

        public override bool Equals(object obj)
            => Equals(obj as VideoModel);

        public bool Equals(VideoModel other) => other != null &&
            Id == other.Id &&
            UserId == other.UserId &&
            Thumbnail.Equals(other.Thumbnail) &&
            Preview.Equals(other.Preview) &&
            Original.Equals(other.Original) &&
            ContentType == other.ContentType &&
            Name == other.Name &&
            Description == other.Description &&
            When == other.When &&
            Location.Equals(other.Location) &&
            Duration == other.Duration;

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Id);
            hash.Add(UserId);
            hash.Add(Thumbnail);
            hash.Add(Preview);
            hash.Add(Original);
            hash.Add(ContentType);
            hash.Add(Name);
            hash.Add(Description);
            hash.Add(When);
            hash.Add(Location);
            hash.Add(Duration);
            return hash.ToHashCode();
        }
    }
}
