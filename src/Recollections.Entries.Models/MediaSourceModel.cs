using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class MediaSourceModel : ICloneable<MediaSourceModel>, IEquatable<MediaSourceModel>
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public MediaSourceModel()
        { }

        public MediaSourceModel(string url, int width, int height)
        {
            Ensure.NotNull(url, "url");
            Ensure.PositiveOrZero(width, "width");
            Ensure.PositiveOrZero(height, "height");
            Url = url;
            Width = width;
            Height = height;
        }

        public MediaSourceModel Clone() => new MediaSourceModel()
        {
            Url = Url,
            Width = Width,
            Height = Height
        };

        public bool Equals(MediaSourceModel other) => other != null &&
            Url == other.Url &&
            Width == other.Width &&
            Height == other.Height;

        public override int GetHashCode() 
            => HashCode.Combine(Url, Width, Height);
    }
}
