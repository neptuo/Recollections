using Neptuo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class ImageSourceModel : ICloneable<ImageSourceModel>, IEquatable<ImageSourceModel>
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public ImageSourceModel()
        { }

        public ImageSourceModel(string url, int width, int height)
        {
            Ensure.NotNull(url, "url");
            Ensure.PositiveOrZero(width, "width");
            Ensure.PositiveOrZero(height, "height");
            Url = url;
            Width = width;
            Height = height;
        }

        public ImageSourceModel Clone() => new ImageSourceModel()
        {
            Url = Url,
            Width = Width,
            Height = Height
        };

        public bool Equals(ImageSourceModel other) => other != null &&
            Url == other.Url &&
            Width == other.Width &&
            Height == other.Height;

        public override int GetHashCode() 
            => HashCode.Combine(Url, Width, Height);
    }
}
