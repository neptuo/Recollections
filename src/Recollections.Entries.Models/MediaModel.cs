using System;

namespace Neptuo.Recollections.Entries
{
    public class MediaModel
    {
        public string Type { get; set; }

        public ImageModel Image { get; set; }
        public VideoModel Video { get; set; }

        public override int GetHashCode()
            => HashCode.Combine(Type, Image, Video);
    }
}
