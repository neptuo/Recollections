using System;
using System.Collections.Generic;

namespace Neptuo.Recollections.Entries
{
    public class EntryTrackModel : ICloneable<EntryTrackModel>, IEquatable<EntryTrackModel>
    {
        public string Data { get; set; }
        public int PointCount { get; set; }
        public double? TotalElevation { get; set; }
        public LocationModel Location { get; set; }

        public bool HasValue()
            => !String.IsNullOrEmpty(Data) && PointCount > 0;

        public EntryTrackModel Clone()
            => new EntryTrackModel()
            {
                Data = Data,
                PointCount = PointCount,
                TotalElevation = TotalElevation,
                Location = Location?.Clone()
            };

        public override bool Equals(object obj)
            => Equals(obj as EntryTrackModel);

        public bool Equals(EntryTrackModel other)
            => other != null
                && Data == other.Data
                && PointCount == other.PointCount
                && TotalElevation == other.TotalElevation
                && EqualityComparer<LocationModel>.Default.Equals(Location, other.Location);

        public override int GetHashCode()
            => HashCode.Combine(Data, PointCount, TotalElevation, Location);
    }
}
