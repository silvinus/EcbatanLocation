using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Tests.ValueObjects;

public class DateRangeTests
{
    [Fact]
    public void Constructor_ValidDates_Creates()
    {
        var range = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

        Assert.Equal(new DateOnly(2026, 7, 1), range.StartDate);
        Assert.Equal(new DateOnly(2026, 7, 8), range.EndDate);
    }

    [Fact]
    public void Constructor_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DateRange(new DateOnly(2026, 7, 8), new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void Constructor_SameDate_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void NumberOfDays_IsCorrect()
    {
        var range = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10));
        Assert.Equal(7, range.NumberOfDays);
    }

    [Fact]
    public void Overlaps_Intersection_ReturnsTrue()
    {
        var a = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));
        var b = new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 15));

        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_Contiguous_ReturnsFalse()
    {
        // H3: departure day excluded, so [1-5) and [5-10) do not overlap
        var a = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var b = new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 10));

        Assert.False(a.Overlaps(b));
        Assert.False(b.Overlaps(a));
    }

    [Fact]
    public void Overlaps_NoIntersection_ReturnsFalse()
    {
        var a = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var b = new DateRange(new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 15));

        Assert.False(a.Overlaps(b));
    }

    [Fact]
    public void Overlaps_Containment_ReturnsTrue()
    {
        var a = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 15));
        var b = new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 10));

        Assert.True(a.Overlaps(b));
        Assert.True(b.Overlaps(a));
    }

    [Fact]
    public void ContainsDay_IncludedDay_ReturnsTrue()
    {
        var range = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10));
        Assert.True(range.ContainsDay(new DateOnly(2026, 7, 3)));
        Assert.True(range.ContainsDay(new DateOnly(2026, 7, 9)));
    }

    [Fact]
    public void ContainsDay_DepartureDay_ReturnsFalse()
    {
        // H3: departure day excluded
        var range = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10));
        Assert.False(range.ContainsDay(new DateOnly(2026, 7, 10)));
    }

    [Fact]
    public void ContainsDay_BeforeRange_ReturnsFalse()
    {
        var range = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10));
        Assert.False(range.ContainsDay(new DateOnly(2026, 7, 2)));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var b = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));

        Assert.Equal(a, b);
    }
}
