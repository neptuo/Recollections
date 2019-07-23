using ExifLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries.Services
{
    public class ImagePropertyReader : DisposableBase
    {
        private readonly ExifReader reader;

        public ImagePropertyReader(string imagePath) 
            => reader = new ExifReader(imagePath);

        public DateTime? FindTakenWhen()
            => Find<DateTime>(ExifTags.DateTimeDigitized);

        public double? FindLatitude() 
            => FindCoordinate(ExifTags.GPSLatitude);

        public double? FindLongitude() 
            => FindCoordinate(ExifTags.GPSLongitude);

        public double? FindAltitude() 
            => FindCoordinate(ExifTags.GPSAltitude);

        private double? FindCoordinate(ExifTags type)
        {
            if (reader.GetTagValue(type, out double[] coordinates))
                return ToDoubleCoordinates(coordinates);

            return null;
        }

        private double ToDoubleCoordinates(double[] coordinates) 
            => coordinates[0] + (coordinates[1] / 60f) + coordinates[2] / 3600f;

        private T? Find<T>(ExifTags type)
            where T : struct
        {
            if (reader.GetTagValue(type, out T value))
                return value;

            return default;
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            reader.Dispose();
        }
    }
}
