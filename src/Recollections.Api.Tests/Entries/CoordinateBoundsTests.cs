using Neptuo.Recollections.Entries;
using Xunit;

namespace Neptuo.Recollections.Tests.Entries;

public class CoordinateBoundsTests
{
    [Fact]
    public void NormalizeLatitude_NonFinite_ReturnsNull()
    {
        Assert.Null(CoordinateBounds.NormalizeLatitude(double.NaN));
        Assert.Null(CoordinateBounds.NormalizeLatitude(double.PositiveInfinity));
        Assert.Null(CoordinateBounds.NormalizeLatitude(double.NegativeInfinity));
    }

    [Fact]
    public void NormalizeLongitude_OutOfRange_ReturnsNull()
    {
        Assert.Null(CoordinateBounds.NormalizeLongitude(181d));
        Assert.Null(CoordinateBounds.NormalizeLongitude(-181d));
    }
}
