using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Queries.EstimateAmount;
using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.Tests.Queries;

public class EstimateAmountQueryHandlerTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_MultipleLines_SumsPerTypeOverNights()
    {
        var query = new EstimateAmountQuery(
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            [
                new PersonLineDto(ClientType.Owner, 2, 0),
                new PersonLineDto(ClientType.Acquaintance, 1, 2),
            ]);

        var total = await Mediator.Send(query);

        // (2*3.5)*7 + (1*15 + 2*7.5)*7 = 49 + 210 = 259
        Assert.Equal(259m, total);
    }

    [Fact]
    public async Task Handle_NonAcquaintanceChildren_ChargedFullRate()
    {
        var query = new EstimateAmountQuery(
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3),
            [new PersonLineDto(ClientType.GuestWithPresence, 1, 1)]);

        var total = await Mediator.Send(query);

        // (1*7 + 1*7) * 2 nights = 28
        Assert.Equal(28m, total);
    }

    [Fact]
    public async Task Handle_NoPricingGridForYear_Throws()
    {
        var query = new EstimateAmountQuery(
            new DateOnly(2030, 7, 1), new DateOnly(2030, 7, 8),
            [new PersonLineDto(ClientType.Owner, 1, 0)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Mediator.Send(query));
    }
}
