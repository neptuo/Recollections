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
        public string SizeText { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string ContentType { get; set; }

        /// <summary>
        /// Absolute URL of the preview-sized media (thumbnail for images, preview poster for videos).
        /// </summary>
        public string PreviewUrl { get; set; }

        /// <summary>
        /// Absolute URL of the original-sized media (used to stream original videos).
        /// </summary>
        public string OriginalUrl { get; set; }

        public override int GetHashCode()
            => HashCode.Combine(Type, Title, SizeText, Width, Height, ContentType, PreviewUrl, OriginalUrl);

        public override bool Equals(object obj)
            => (obj is GalleryModel other) && Equals(other);

        public bool Equals(GalleryModel other)
            => Type == other.Type
                && Title == other.Title
                && SizeText == other.SizeText
                && Width == other.Width
                && Height == other.Height
                && ContentType == other.ContentType
                && PreviewUrl == other.PreviewUrl
                && OriginalUrl == other.OriginalUrl;
    }
}
