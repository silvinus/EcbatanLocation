using EcbatanLocation.Application.Commands.AcceptReservation;
using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.Commands.DeleteReservation;
using EcbatanLocation.Application.Commands.UpdateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Commands;

public class HypotheticalReservationTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateOnly Start = new(2026, 7, 5);
    private static readonly DateOnly End = new(2026, 7, 12);

    [Fact]
    public async Task Create_Hypothetical_OverTakenLodging_Succeeds()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Real", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        // Same dates, same lodging: would normally be rejected, but the hypothetical flag bypasses it.
        var hypoId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Hypo", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: true));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var hypo = await repo.GetByIdAsync(hypoId);
        Assert.NotNull(hypo);
        Assert.True(hypo.IsHypothetical);
        Assert.Equal(ReservationStatus.Pending, hypo.Status);
    }

    [Fact]
    public async Task Accept_Hypothetical_Throws()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        var hypoId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Hypo", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: true));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Mediator.Send(new AcceptReservationCommand(hypoId, "Léa")));
    }

    [Fact]
    public async Task Hypothetical_DoesNotBlockRealReservation()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Hypo", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: true));

        // A real reservation on the very same slot must still be allowed (the hypothetical is invisible).
        var realId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Real", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        var repo = Services.GetRequiredService<IReservationRepository>();
        Assert.NotNull(await repo.GetByIdAsync(realId));
    }

    [Fact]
    public async Task Delete_RealReservation_SingleHypothetical_AutoPromotes()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        var realId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Real", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        var hypoId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Hypo", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: true));

        await Mediator.Send(new DeleteReservationCommand(realId));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var hypo = await repo.GetByIdAsync(hypoId);
        Assert.NotNull(hypo);
        Assert.False(hypo.IsHypothetical);
        Assert.Equal(ReservationStatus.Pending, hypo.Status);
    }

    [Fact]
    public async Task Delete_RealReservation_MultipleHypotheticals_StayHypothetical()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        var realId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Real", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        var hypo1 = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Hypo1", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: true));
        var hypo2 = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Hypo2", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: true));

        await Mediator.Send(new DeleteReservationCommand(realId));

        var repo = Services.GetRequiredService<IReservationRepository>();
        // Two candidates contend for the freed slot, so neither is auto-promoted (manual resolution).
        Assert.True((await repo.GetByIdAsync(hypo1))!.IsHypothetical);
        Assert.True((await repo.GetByIdAsync(hypo2))!.IsHypothetical);
    }

    [Fact]
    public async Task Update_UntickHypothetical_PromotesWhenSlotFree()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        var hypoId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id, Start, End,
            "Hypo", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: true));

        // Manual promotion: the slot is free, so clearing the flag must pass the availability checks.
        await Mediator.Send(new UpdateReservationCommand(hypoId, villa.Id, Start, End,
            "Hypo", [new PersonLineDto(ClientType.Owner, 2, 0)], IsHypothetical: false));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var promoted = await repo.GetByIdAsync(hypoId);
        Assert.NotNull(promoted);
        Assert.False(promoted.IsHypothetical);
    }
}
