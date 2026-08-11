using Neptuo.Recollections.Entries;
using Xunit;

namespace Neptuo.Recollections.Tests.Entries;

public class BeingTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(6, 15, true)]
    [InlineData(2, 29, true)]
    [InlineData(2, 30, false)]
    [InlineData(13, 1, false)]
    [InlineData(6, null, false)]
    public void HasValidNameDay_ValidatesMonthAndDay(int? month, int? day, bool expected)
    {
        var being = new Being
        {
            NameDayMonth = month,
            NameDayDay = day
        };

        Assert.Equal(expected, being.HasValidNameDay());
    }
}
