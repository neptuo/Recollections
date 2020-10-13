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

        public string Thumbnail { get; set; }
        public string Preview { get; set; }
        public string Original{ get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime When { get; set; }

        public LocationModel Location { get; set; } = new LocationModel();

        public ImageModel Clone() => new ImageModel()
        {
            Id = Id,
            UserId = UserId,
            Thumbnail = Thumbnail,
            Preview = Preview,
            Original = Original,
            Name = Name,
            Description = Description,
            When = When,
            Location = Location.Clone()
        };

        public override bool Equals(object obj) => Equals(obj as ImageModel);

        public bool Equals(ImageModel other) => other != null &&
            Id == other.Id &&
            UserId == other.UserId && 
            Thumbnail == other.Thumbnail &&
            Preview == other.Preview &&
            Original == other.Original &&
            Name == other.Name &&
            Description == other.Description &&
            When == other.When &&
            Location.Equals(other.Location);

        public override int GetHashCode()
        {
            var hashCode = 3;
            hashCode = hashCode * 7 + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * 7 + EqualityComparer<string>.Default.GetHashCode(UserId);
            hashCode = hashCode * 7 + EqualityComparer<string>.Default.GetHashCode(Thumbnail);
            hashCode = hashCode * 7 + EqualityComparer<string>.Default.GetHashCode(Preview);
            hashCode = hashCode * 7 + EqualityComparer<string>.Default.GetHashCode(Original);
            hashCode = hashCode * 7 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * 7 + EqualityComparer<string>.Default.GetHashCode(Description);
            hashCode = hashCode * 7 + When.GetHashCode();
            hashCode = hashCode * 7 + Location.GetHashCode();
            return hashCode;
        }
    }
}
