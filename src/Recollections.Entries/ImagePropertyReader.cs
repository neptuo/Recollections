using ExifLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public partial class ImagePropertyReader : DisposableBase
    {
        private readonly ExifReader reader;

        public ImagePropertyReader(string imagePath)
        {
            try
            {
                reader = new ExifReader(imagePath);
            }
            catch (ExifLibException)
            {
                reader = null;
            }
        }

        public ImagePropertyReader(Stream imageContent)
        {
            try
            {
                reader = new ExifReader(imageContent);
            }
            catch (ExifLibException)
            {
                reader = null;
            }
        }

        public Orientation? FindOrientation()
        {
            ushort? value = Find<ushort>(ExifTags.Orientation);
            if (value == null)
                return null;

            return (Orientation)value.Value;
        }

        public DateTime? FindTakenWhen()
            => Find<DateTime>(ExifTags.DateTimeDigitized)
            ?? Find<DateTime>(ExifTags.DateTimeOriginal)
            ?? Find<DateTime>(ExifTags.DateTime);

        public double? FindLatitude()
            => FindCoordinate(ExifTags.GPSLatitude);

        public double? FindLongitude()
            => FindCoordinate(ExifTags.GPSLongitude);

        public double? FindAltitude()
        {
            if (reader == null)
                return null;

            if (!TryGetTagValue(ExifTags.GPSAltitude, out uint[] value)
                || value == null || value.Length < 2 || value[1] == 0)
            {
                return null;
            }

            double altitudeMeters = value[0] / (double)value[1];

            // GPSAltitudeRef: 0 = above sea level, 1 = below
            if (TryGetTagValue(ExifTags.GPSAltitudeRef, out byte altRef) && altRef == 1)
                altitudeMeters = -altitudeMeters;

            return altitudeMeters;
        }

        private double? FindCoordinate(ExifTags type)
        {
            if (reader == null)
                return null;

            if (!TryGetTagValue(type, out double[] coordinates) || coordinates == null || coordinates.Length < 3)
                return null;

            double value = ToDoubleCoordinates(coordinates);
            return type switch
            {
                ExifTags.GPSLatitude => CoordinateBounds.NormalizeLatitude(value),
                ExifTags.GPSLongitude => CoordinateBounds.NormalizeLongitude(value),
                _ => double.IsFinite(value) ? value : null
            };
        }

        private double ToDoubleCoordinates(double[] coordinates)
            => Math.Round(coordinates[0] + (coordinates[1] / 60f) + coordinates[2] / 3600f, 13);

        private T? Find<T>(ExifTags type)
            where T : struct
        {
            if (reader == null)
                return null;

            if (TryGetTagValue(type, out T value))
                return value;

            return default;
        }

        private bool TryGetTagValue<T>(ExifTags type, out T value)
        {
            if (reader != null)
            {
                try
                {
                    if (reader.GetTagValue(type, out value))
                        return true;
                }
                catch (FormatException)
                { }
                catch (InvalidCastException)
                { }
            }

            value = default;
            return false;
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();

            if (reader != null)
                reader.Dispose();
        }
    }
}
