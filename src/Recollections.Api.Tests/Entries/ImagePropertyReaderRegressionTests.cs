using Neptuo.Recollections.Entries;
using Xunit;

namespace Neptuo.Recollections.Tests.Entries;

public class ImagePropertyReaderRegressionTests
{
    [Fact]
    public void RegressionImage_CanBeReadWithoutExifExceptions()
    {
        using var stream = File.OpenRead(GetRegressionImagePath());
        using var reader = new ImagePropertyReader(stream);

        var latitude = reader.FindLatitude();
        var longitude = reader.FindLongitude();
        var altitude = reader.FindAltitude();

        Assert.Equal(50.0111233d, latitude);
        Assert.Equal(14.6262635d, longitude);
        Assert.True(latitude is double lat && double.IsFinite(lat));
        Assert.True(longitude is double lon && double.IsFinite(lon));
        Assert.True(altitude == null || AltitudeBounds.IsValid(altitude));
    }

    private static string GetRegressionImagePath()
        => Path.Combine(AppContext.BaseDirectory, "TestData", "Images", "20260423_073316.jpg");
}
