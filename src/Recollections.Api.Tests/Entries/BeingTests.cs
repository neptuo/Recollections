using Neptuo.Recollections.Entries;
using Neptuo.Recollections.Entries.Beings;
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

    [Fact]
    public void Create_ReturnsUpcomingBirthdaysAndNameDaysInDateOrder()
    {
        var beings = new[]
        {
            new Being { Id = "birthday", Name = "Birthday", BirthDate = new DateTime(1990, 8, 20) },
            new Being { Id = "name-day", Name = "Name day", NameDayMonth = 8, NameDayDay = 15 }
        };

        List<UpcomingBeingModel> result = UpcomingBeingUtils.Create(beings, new DateTime(2026, 8, 12), true, true);

        Assert.Equal(2, result.Count);
        Assert.Equal("name-day", result[0].Id);
        Assert.Equal(new DateTime(2026, 8, 15), result[0].Date);
        Assert.False(result[0].IsBirthday);
        Assert.Equal("birthday", result[1].Id);
        Assert.Equal(36, result[1].Age);
    }

    [Fact]
    public void Create_UsesNextYearAndCalculatesBirthdayAge()
    {
        var being = new Being { Id = "birthday", Name = "Birthday", BirthDate = new DateTime(2000, 2, 29) };

        List<UpcomingBeingModel> result = UpcomingBeingUtils.Create([being], new DateTime(2026, 3, 1), true, false);

        Assert.Equal(new DateTime(2027, 2, 28), result[0].Date);
        Assert.Equal(27, result[0].Age);
    }
}
