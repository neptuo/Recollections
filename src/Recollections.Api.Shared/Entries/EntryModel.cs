using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class EntryModel : ICloneable<EntryModel>, IEquatable<EntryModel>
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime When { get; set; }
        public string Text { get; set; }

        public List<LocationModel> Locations { get; set; } = new List<LocationModel>();

        public EntryModel()
        { }

        public EntryModel(string title, DateTime when)
        {
            Title = title;
            When = when;
        }

        public EntryModel(string id, string userId, string title, DateTime when, string text)
        {
            Id = id;
            Title = title;
            When = when;
            Text = text;
        }

        public EntryModel Clone()
        {
            var clone = new EntryModel()
            {
                Id = Id,
                Title = Title,
                When = When,
                Text = Text
            };

            clone.Locations.AddRange(Locations.Select(l => l.Clone()));
            return clone;
        }

        public override bool Equals(object obj) => Equals(obj as EntryModel);

        public bool Equals(EntryModel other) => other != null &&
            Id == other.Id &&
            Title == other.Title &&
            When == other.When &&
            Text == other.Text &&
            EqualsLocations(other.Locations);

        protected bool EqualsLocations(List<LocationModel> other)
        {
            if (Locations.Count != other.Count)
                return false;

            for (int i = 0; i < Locations.Count; i++)
            {
                var a = Locations[i];
                var b = other[i];
                if (!a.Equals(b))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = 242076647;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Title);
            hashCode = hashCode * -1521134295 + When.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
            hashCode = hashCode * -1521134295 + EqualityComparer<ICollection<LocationModel>>.Default.GetHashCode(Locations);
            return hashCode;
        }
    }
}
