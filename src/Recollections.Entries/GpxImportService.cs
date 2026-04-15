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
        private const double EarthRadius = 6371000d;

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
            catch (TrackImportValidationException)
            {
                throw;
            }
            catch (Exception ex) when (ex is XmlException || ex is IOException || ex is InvalidOperationException)
            {
                throw new TrackImportValidationException();
            }
        }

        public EntryTrackModel Create(IReadOnlyList<LocationModel> locations)
        {
            Ensure.NotNull(locations, "locations");
            EnsureHasCoordinates(locations);

            IReadOnlyList<LocationModel> normalized = Normalize(locations);
            IReadOnlyList<LocationModel> simplified = Simplify(normalized);
            if (simplified.Count == 0)
                throw new TrackImportValidationException();

            return new EntryTrackModel()
            {
                Data = Encode(simplified),
                PointCount = simplified.Count,
                TotalElevation = CalculateTotalElevation(normalized),
                TotalDistance = CalculateTotalDistance(normalized),
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
                if (!TryDecodeNextCoordinate(data, ref index, out int latitudeDelta))
                    return [];

                if (!TryDecodeNextCoordinate(data, ref index, out int longitudeDelta))
                    return [];

                latitude += latitudeDelta;
                longitude += longitudeDelta;

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

        private static void EnsureHasCoordinates(IReadOnlyList<LocationModel> locations)
        {
            if (locations.Any(l => l == null || !l.HasValue()))
                throw new TrackImportValidationException();
        }

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

        private static bool TryDecodeNextCoordinate(string data, ref int index, out int coordinate)
        {
            coordinate = 0;
            if (index >= data.Length)
                return false;

            int result = 0;
            int shift = 0;
            while (true)
            {
                if (index >= data.Length)
                    return false;

                int value = data[index++] - 63;
                if (value < 0)
                    return false;

                result |= (value & 0x1f) << shift;
                shift += 5;

                if (value < 0x20)
                    break;
            }

            coordinate = (result & 1) == 1 ? ~(result >> 1) : (result >> 1);
            return true;
        }

        private static IReadOnlyList<LocationModel> Simplify(IReadOnlyList<LocationModel> locations)
        {
            IReadOnlyList<LocationModel> uniqueLocations = Normalize(locations);

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

        private static IReadOnlyList<LocationModel> Normalize(IReadOnlyList<LocationModel> locations)
        {
            List<LocationModel> uniqueLocations = new List<LocationModel>(locations.Count);
            foreach (var location in locations)
            {
                if (uniqueLocations.Count == 0 || !uniqueLocations[uniqueLocations.Count - 1].Equals(location))
                    uniqueLocations.Add(location.Clone());
            }

            return uniqueLocations;
        }

        private static double? CalculateTotalElevation(IReadOnlyList<LocationModel> locations)
        {
            double total = 0;
            double? previousAltitude = null;
            bool hasAltitude = false;

            foreach (var location in locations)
            {
                if (location?.Altitude == null)
                    continue;

                hasAltitude = true;
                double currentAltitude = location.Altitude.Value;
                if (previousAltitude != null)
                {
                    double delta = currentAltitude - previousAltitude.Value;
                    if (delta > 0)
                        total += delta;
                }

                previousAltitude = currentAltitude;
            }

            if (!hasAltitude)
                return null;

            return Math.Round(total, 1, MidpointRounding.AwayFromZero);
        }

        private static double CalculateTotalDistance(IReadOnlyList<LocationModel> locations)
        {
            double total = 0;
            for (int i = 1; i < locations.Count; i++)
                total += CalculateDistance(locations[i - 1], locations[i]);

            return Math.Round(total, 1, MidpointRounding.AwayFromZero);
        }

        private static double CalculateDistance(LocationModel previous, LocationModel current)
        {
            double latitude1 = ToRadians(previous.Latitude.Value);
            double latitude2 = ToRadians(current.Latitude.Value);
            double latitudeDelta = latitude2 - latitude1;
            double longitudeDelta = ToRadians(current.Longitude.Value - previous.Longitude.Value);

            double a =
                Math.Pow(Math.Sin(latitudeDelta / 2d), 2d) +
                Math.Cos(latitude1) * Math.Cos(latitude2) * Math.Pow(Math.Sin(longitudeDelta / 2d), 2d);
            a = Math.Min(1d, a);
            double c = 2d * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1d - a));
            double horizontalDistance = EarthRadius * c;

            if (previous.Altitude != null && current.Altitude != null)
            {
                double altitudeDelta = current.Altitude.Value - previous.Altitude.Value;
                return Math.Sqrt((horizontalDistance * horizontalDistance) + (altitudeDelta * altitudeDelta));
            }

            return horizontalDistance;
        }

        private static double ToRadians(double degrees)
            => degrees * (Math.PI / 180d);
    }
}
