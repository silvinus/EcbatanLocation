using PlanningLocation.Application.DTOs;
using PlanningLocation.Application.Queries.EstimateAmount;
using PlanningLocation.Application.Tests.Fakes;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Application.Tests.Queries;

public class EstimateAmountQueryHandlerTests
{
    private readonly FakePricingGridRepository _grids = new();

    private EstimateAmountQueryHandler CreateHandler()
    {
        _grids.Items.Add(PricingGrid.Create(2026,
        [
            PricingLine.Create(ClientType.Owner, 3.50m),
            PricingLine.Create(ClientType.GuestWithPresence, 7.00m),
            PricingLine.Create(ClientType.Acquaintance, 15.00m),
        ]));
        return new EstimateAmountQueryHandler(_grids);
    }

    [Fact]
    public async Task Handle_MultipleLines_SumsPerTypeOverNights()
    {
        var handler = CreateHandler();
        // 7 nights. Owner: 2 adults * 3.50. Acquaintance: 1 adult * 15 + 2 children * (15 * 0.5).
        var query = new EstimateAmountQuery(
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            [
                new PersonLineDto(ClientType.Owner, 2, 0),
                new PersonLineDto(ClientType.Acquaintance, 1, 2),
            ]);

        var total = await handler.Handle(query, default);

        // (2*3.5)*7 + (1*15 + 2*7.5)*7 = 49 + 210 = 259
        Assert.Equal(259m, total);
    }

    [Fact]
    public async Task Handle_NonAcquaintanceChildren_ChargedFullRate()
    {
        var handler = CreateHandler();
        // GuestWithPresence children are charged the full per-person rate (no discount).
        var query = new EstimateAmountQuery(
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3),
            [new PersonLineDto(ClientType.GuestWithPresence, 1, 1)]);

        var total = await handler.Handle(query, default);

        // (1*7 + 1*7) * 2 nights = 28
        Assert.Equal(28m, total);
    }

    [Fact]
    public async Task Handle_NoPricingGridForYear_Throws()
    {
        var handler = new EstimateAmountQueryHandler(_grids); // empty repo
        var query = new EstimateAmountQuery(
            new DateOnly(2030, 7, 1), new DateOnly(2030, 7, 8),
            [new PersonLineDto(ClientType.Owner, 1, 0)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(query, default));
    }
}
