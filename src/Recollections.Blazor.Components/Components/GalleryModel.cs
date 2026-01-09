using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Components
{
    public class GalleryModel : IEquatable<GalleryModel>
    {
        /// <summary>
        /// Optional discriminator, e.g. "image" or "video".
        /// </summary>
        public string Type { get; set; }

        public string Title { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string ContentType { get; set; }

        public override int GetHashCode()
            => HashCode.Combine(Type, Title, Width, Height, ContentType);

        public override bool Equals(object obj)
            => (obj is GalleryModel other) && Equals(other);

        public bool Equals(GalleryModel other)
            => Type == other.Type
                && Title == other.Title
                && Width == other.Width
                && Height == other.Height
                && ContentType == other.ContentType;
    }
}
