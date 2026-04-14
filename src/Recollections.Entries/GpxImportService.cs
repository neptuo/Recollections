using Neptuo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Neptuo.Recollections.Entries
{
    public class GpxImportService
    {
        public const int MaxLocationCount = 250;

        public EntryTrackModel Parse(IFileInput input)
        {
            Ensure.NotNull(input, "input");

            string extension = Path.GetExtension(input.FileName);
            if (!String.Equals(extension, ".gpx", StringComparison.OrdinalIgnoreCase))
                throw new TrackImportValidationException();

            try
            {
                using Stream stream = input.OpenReadStream();
                XDocument document = XDocument.Load(stream);

                List<LocationModel> result = document
                    .Descendants()
                    .Where(e => e.Name.LocalName == "trkpt" || e.Name.LocalName == "rtept")
                    .Select(MapLocation)
                    .Where(l => l != null && l.HasValue())
                    .ToList();

                if (result.Count == 0)
                    throw new TrackImportValidationException();

                return Create(result);
            }
            catch (XmlException)
            {
                throw new TrackImportValidationException();
            }
        }

        public EntryTrackModel Create(IReadOnlyList<LocationModel> locations)
        {
            Ensure.NotNull(locations, "locations");

            IReadOnlyList<LocationModel> simplified = Simplify(locations);
            if (simplified.Count == 0)
                throw new TrackImportValidationException();

            return new EntryTrackModel()
            {
                Data = Encode(simplified),
                PointCount = simplified.Count,
                Location = simplified[simplified.Count / 2].Clone()
            };
        }

        public IReadOnlyList<LocationModel> Decode(string data)
        {
            if (String.IsNullOrEmpty(data))
                return [];

            List<LocationModel> result = [];
            int index = 0;
            int latitude = 0;
            int longitude = 0;

            while (index < data.Length)
            {
                latitude += DecodeNextCoordinate(data, ref index);
                longitude += DecodeNextCoordinate(data, ref index);

                result.Add(new LocationModel()
                {
                    Latitude = latitude / 100000d,
                    Longitude = longitude / 100000d
                });
            }

            return result;
        }

        private static LocationModel MapLocation(XElement element)
        {
            if (!TryParseDouble(element.Attribute("lat")?.Value, out double latitude))
                return null;

            if (!TryParseDouble(element.Attribute("lon")?.Value, out double longitude))
                return null;

            double? altitude = null;
            XElement altitudeElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "ele");
            if (altitudeElement != null && TryParseDouble(altitudeElement.Value, out double altitudeValue))
                altitude = altitudeValue;

            return new LocationModel()
            {
                Latitude = latitude,
                Longitude = longitude,
                Altitude = altitude
            };
        }

        private static bool TryParseDouble(string value, out double result)
            => Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

        private static string Encode(IReadOnlyList<LocationModel> locations)
        {
            var builder = new System.Text.StringBuilder();
            int previousLatitude = 0;
            int previousLongitude = 0;

            foreach (var location in locations)
            {
                int latitude = (int)Math.Round(location.Latitude.Value * 100000d, MidpointRounding.AwayFromZero);
                int longitude = (int)Math.Round(location.Longitude.Value * 100000d, MidpointRounding.AwayFromZero);

                EncodeCoordinate(builder, latitude - previousLatitude);
                EncodeCoordinate(builder, longitude - previousLongitude);

                previousLatitude = latitude;
                previousLongitude = longitude;
            }

            return builder.ToString();
        }

        private static void EncodeCoordinate(System.Text.StringBuilder builder, int value)
        {
            value = value < 0 ? ~(value << 1) : (value << 1);

            while (value >= 0x20)
            {
                builder.Append((char)((0x20 | (value & 0x1f)) + 63));
                value >>= 5;
            }

            builder.Append((char)(value + 63));
        }

        private static int DecodeNextCoordinate(string data, ref int index)
        {
            int result = 0;
            int shift = 0;
            int value;

            do
            {
                value = data[index++] - 63;
                result |= (value & 0x1f) << shift;
                shift += 5;
            }
            while (value >= 0x20 && index < data.Length);

            return (result & 1) == 1 ? ~(result >> 1) : (result >> 1);
        }

        private static IReadOnlyList<LocationModel> Simplify(IReadOnlyList<LocationModel> locations)
        {
            List<LocationModel> uniqueLocations = new List<LocationModel>(locations.Count);
            foreach (var location in locations)
            {
                if (uniqueLocations.Count == 0 || !uniqueLocations[uniqueLocations.Count - 1].Equals(location))
                    uniqueLocations.Add(location);
            }

            if (uniqueLocations.Count <= MaxLocationCount)
                return uniqueLocations;

            List<LocationModel> result = new List<LocationModel>(MaxLocationCount);
            double step = (uniqueLocations.Count - 1d) / (MaxLocationCount - 1d);
            for (int i = 0; i < MaxLocationCount; i++)
            {
                int index = (int)Math.Round(i * step, MidpointRounding.AwayFromZero);
                index = Math.Min(index, uniqueLocations.Count - 1);

                LocationModel location = uniqueLocations[index];
                if (result.Count == 0 || !result[result.Count - 1].Equals(location))
                    result.Add(location.Clone());
            }

            LocationModel last = uniqueLocations[uniqueLocations.Count - 1];
            if (!result[result.Count - 1].Equals(last))
                result[result.Count - 1] = last.Clone();

            return result;
        }
    }
}
