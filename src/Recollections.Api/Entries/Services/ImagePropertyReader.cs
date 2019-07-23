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

        public double? GetLatitude() 
            => GetCoordinate(ExifTags.GPSLatitude);

        public double? GetLongitude() 
            => GetCoordinate(ExifTags.GPSLongitude);

        public double? GetAltitude() 
            => GetCoordinate(ExifTags.GPSAltitude);

        private double? GetCoordinate(ExifTags type)
        {
            if (reader.GetTagValue(type, out double[] coordinates))
                return ToDoubleCoordinates(coordinates);

            return null;
        }

        private double ToDoubleCoordinates(double[] coordinates) 
            => coordinates[0] + (coordinates[1] / 60f) + coordinates[2] / 3600f;

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            reader.Dispose();
        }
    }
}
