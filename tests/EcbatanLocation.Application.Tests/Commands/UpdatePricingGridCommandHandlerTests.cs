using EcbatanLocation.Application.Commands.UpdatePricingGrid;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Commands;

public class UpdatePricingGridCommandHandlerTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private static UpdatePricingGridCommand Command(int year) => new(year,
    [
        new PricingLineInput(ClientType.Owner, 4.00m),
        new PricingLineInput(ClientType.Acquaintance, 16.00m),
    ]);

    [Fact]
    public async Task Handle_NoExistingGrid_CreatesNewGrid()
    {
        AuthState.SetAdmin();

        await Mediator.Send(Command(2030));

        var repo = Services.GetRequiredService<IPricingGridRepository>();
        var grid = await repo.GetByYearAsync(2030);
        Assert.NotNull(grid);
        Assert.Equal(4.00m, grid.GetRate(ClientType.Owner));
    }

    [Fact]
    public async Task Handle_ExistingGrid_UpdatesItInPlace()
    {
        AuthState.SetAdmin();

        await Mediator.Send(Command(2026));

        var repo = Services.GetRequiredService<IPricingGridRepository>();
        var grid = await repo.GetByYearAsync(2026);
        Assert.NotNull(grid);
        Assert.Equal(4.00m, grid.GetRate(ClientType.Owner));
    }
}
