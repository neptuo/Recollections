using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neptuo.Recollections.Entries;

public class CountryService
{
    private readonly GeoJsonFeatureCollection countries;

    public CountryService()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("Neptuo.Recollections.Entries.countries.50m.geo.json");
        countries = JsonSerializer.Deserialize<GeoJsonFeatureCollection>(stream);
    }

    public GeoJsonFeatureCollection GetVisitedCountries(List<MapEntryModel> entries)
    {
        var visitedIndices = new HashSet<int>();

        foreach (var entry in entries)
        {
            if (entry.Location?.Latitude == null || entry.Location?.Longitude == null)
                continue;

            double lat = entry.Location.Latitude.Value;
            double lng = entry.Location.Longitude.Value;

            for (int i = 0; i < countries.Features.Count; i++)
            {
                if (visitedIndices.Contains(i))
                    continue;

                if (PointInGeometry(lat, lng, countries.Features[i].Geometry))
                {
                    visitedIndices.Add(i);
                    break;
                }
            }
        }

        var result = new GeoJsonFeatureCollection
        {
            Features = new List<GeoJsonFeature>()
        };

        foreach (var index in visitedIndices)
            result.Features.Add(countries.Features[index]);

        return result;
    }

    private static bool PointInGeometry(double lat, double lng, GeoJsonGeometry geometry)
    {
        if (geometry.Type == "Polygon")
        {
            return PointInPolygon(lat, lng, geometry.Coordinates[0]);
        }
        else if (geometry.Type == "MultiPolygon")
        {
            foreach (var polygon in geometry.MultiCoordinates)
            {
                if (PointInPolygon(lat, lng, polygon[0]))
                    return true;
            }
        }

        return false;
    }

    private static bool PointInPolygon(double lat, double lng, List<List<double>> ring)
    {
        bool inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            double xi = ring[i][1], yi = ring[i][0];
            double xj = ring[j][1], yj = ring[j][0];

            bool intersect = ((yi > lng) != (yj > lng))
                && (lat < (xj - xi) * (lng - yi) / (yj - yi) + xi);

            if (intersect)
                inside = !inside;
        }

        return inside;
    }
}

public class GeoJsonFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "FeatureCollection";

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; set; }
}

public class GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Feature";

    [JsonPropertyName("properties")]
    public GeoJsonProperties Properties { get; set; }

    [JsonPropertyName("geometry")]
    public GeoJsonGeometry Geometry { get; set; }
}

public class GeoJsonProperties
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("iso")]
    public string Iso { get; set; }
}

public class GeoJsonGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("coordinates")]
    public JsonElement RawCoordinates { get; set; }

    private List<List<List<double>>> _coordinates;
    private List<List<List<List<double>>>> _multiCoordinates;

    /// <summary>
    /// Parsed coordinates for Polygon type.
    /// </summary>
    [JsonIgnore]
    public List<List<List<double>>> Coordinates
    {
        get
        {
            if (_coordinates == null && Type == "Polygon")
                _coordinates = JsonSerializer.Deserialize<List<List<List<double>>>>(RawCoordinates.GetRawText());

            return _coordinates;
        }
    }

    /// <summary>
    /// Parsed coordinates for MultiPolygon type.
    /// </summary>
    [JsonIgnore]
    public List<List<List<List<double>>>> MultiCoordinates
    {
        get
        {
            if (_multiCoordinates == null && Type == "MultiPolygon")
                _multiCoordinates = JsonSerializer.Deserialize<List<List<List<List<double>>>>>(RawCoordinates.GetRawText());

            return _multiCoordinates;
        }
    }
}
