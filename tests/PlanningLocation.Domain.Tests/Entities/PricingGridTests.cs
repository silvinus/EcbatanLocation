using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Domain.Tests.Entities;

public class PricingGridTests
{
    private static PricingGrid CreateGrid2026()
    {
        return PricingGrid.Create(2026,
        [
            PricingLine.Create(ClientType.Owner, 3.50m),
            PricingLine.Create(ClientType.GuestWithPresence, 7.00m),
            PricingLine.Create(ClientType.Acquaintance, 15.00m),
            PricingLine.Create(ClientType.MobileHome, 12.00m),
            PricingLine.Create(ClientType.Tent, 7.00m),
        ]);
    }

    [Fact]
    public void GetRate_ExistingType_ReturnsPrice()
    {
        var grid = CreateGrid2026();

        Assert.Equal(3.50m, grid.GetRate(ClientType.Owner));
        Assert.Equal(15.00m, grid.GetRate(ClientType.Acquaintance));
    }

    [Fact]
    public void GetRate_MissingType_Throws()
    {
        var grid = PricingGrid.Create(2026, []);

        Assert.Throws<InvalidOperationException>(() =>
            grid.GetRate(ClientType.Owner));
    }

    [Fact]
    public void Update_ReplacesLines()
    {
        var grid = CreateGrid2026();

        grid.Update([PricingLine.Create(ClientType.Owner, 5.00m)]);

        Assert.Equal(5.00m, grid.GetRate(ClientType.Owner));
        Assert.Single(grid.Lines);
    }

    [Fact]
    public void Create_YearAndLinesAreCorrect()
    {
        var grid = CreateGrid2026();

        Assert.Equal(2026, grid.Year);
        Assert.Equal(5, grid.Lines.Count);
    }
}
