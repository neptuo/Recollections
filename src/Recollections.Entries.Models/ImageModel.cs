using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class ImageModel : ICloneable<ImageModel>, IEquatable<ImageModel>
    {
        public string Id { get; set; }
        public string UserId { get; set; }

        public MediaSourceModel Thumbnail { get; set; }
        public MediaSourceModel Preview { get; set; }
        public MediaSourceModel Original { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime When { get; set; }

        public LocationModel Location { get; set; } = new LocationModel();

        public ImageModel Clone() => new ImageModel()
        {
            Id = Id,
            UserId = UserId,
            Thumbnail = Thumbnail.Clone(),
            Preview = Preview.Clone(),
            Original = Original.Clone(),
            Name = Name,
            Description = Description,
            When = When,
            Location = Location.Clone()
        };

        public override bool Equals(object obj) 
            => Equals(obj as ImageModel);

        public bool Equals(ImageModel other) => other != null &&
            Id == other.Id &&
            UserId == other.UserId && 
            Thumbnail.Equals(other.Thumbnail) &&
            Preview.Equals(other.Preview) &&
            Original.Equals(other.Original) &&
            Name == other.Name &&
            Description == other.Description &&
            When == other.When &&
            Location.Equals(other.Location);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Id);
            hash.Add(UserId);
            hash.Add(Thumbnail);
            hash.Add(Preview);
            hash.Add(Original);
            hash.Add(Name);
            hash.Add(Description);
            hash.Add(When);
            hash.Add(Location);
            return hash.ToHashCode();
        }
    }
}
