using Neptuo.Recollections.Entries;
using Xunit;

namespace Neptuo.Recollections.Tests.Entries;

/// <summary>
/// Validates that ImagePropertyReader correctly parses EXIF GPS data and produces finite, in-range coordinates.
/// Uses synthetic fixture to avoid shipping personal data.
/// </summary>
[Collection(nameof(TestFixtureCollection))]
public class ImagePropertyReaderRegressionTests
{
    [Fact]
    public void SyntheticExifImage_CanBeReadWithFiniteCoordinates()
    {
        using var stream = File.OpenRead(GetSyntheticImagePath());
        using var reader = new ImagePropertyReader(stream);

        var latitude = reader.FindLatitude();
        var longitude = reader.FindLongitude();
        var altitude = reader.FindAltitude();

        Assert.NotNull(latitude);
        Assert.NotNull(longitude);
        Assert.NotNull(altitude);
        Assert.Equal(10.5d, latitude.Value, precision: 2);
        Assert.Equal(20.75d, longitude.Value, precision: 2);
        Assert.Equal(150.0d, altitude.Value, precision: 1);
        Assert.True(double.IsFinite(latitude.Value));
        Assert.True(double.IsFinite(longitude.Value));
        Assert.True(AltitudeBounds.IsValid(altitude));
    }

    private static string GetSyntheticImagePath()
        => Path.Combine(AppContext.BaseDirectory, "TestData", "Images", "synthetic-exif-gps.jpg");
}
