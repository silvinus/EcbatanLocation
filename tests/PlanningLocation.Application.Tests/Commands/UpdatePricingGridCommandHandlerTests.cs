using PlanningLocation.Application.Commands.UpdatePricingGrid;
using PlanningLocation.Application.Tests.Fakes;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Tests.Commands;

public class UpdatePricingGridCommandHandlerTests
{
    private readonly FakePricingGridRepository _grids = new();

    private UpdatePricingGridCommandHandler CreateHandler() => new(_grids);

    private static UpdatePricingGridCommand Command(int year) => new(year,
    [
        new PricingLineInput(ClientType.Owner, 4.00m),
        new PricingLineInput(ClientType.Acquaintance, 16.00m),
    ]);

    [Fact]
    public async Task Handle_NoExistingGrid_CreatesNewGrid()
    {
        var handler = CreateHandler();

        await handler.Handle(Command(2027), default);

        var grid = Assert.Single(_grids.Items);
        Assert.Equal(2027, grid.Year);
        Assert.Equal(1, _grids.AddCount);
        Assert.Equal(0, _grids.UpdateCount);
        Assert.Equal(4.00m, grid.GetRate(ClientType.Owner));
    }

    [Fact]
    public async Task Handle_ExistingGrid_UpdatesItInPlace()
    {
        _grids.Items.Add(PricingGrid.Create(2027, [PricingLine.Create(ClientType.Owner, 3.50m)]));
        var handler = CreateHandler();

        await handler.Handle(Command(2027), default);

        Assert.Single(_grids.Items);
        Assert.Equal(0, _grids.AddCount);
        Assert.Equal(1, _grids.UpdateCount);
        Assert.Equal(4.00m, _grids.Items[0].GetRate(ClientType.Owner));
    }
}
