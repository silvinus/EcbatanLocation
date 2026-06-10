using EcbatanLocation.Application.Commands.AcceptReservation;
using EcbatanLocation.Application.Commands.ConfirmReservation;
using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.Commands.DeleteReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Commands;

public class ReservationStatusCommandHandlerTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    private async Task<Guid> SeedReservationAsync()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");
        return await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]));
    }

    [Fact]
    public async Task Accept_PendingReservation_MovesToAccepted()
    {
        var id = await SeedReservationAsync();

        await Mediator.Send(new AcceptReservationCommand(id, "Léa"));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var reservation = await repo.GetByIdAsync(id);
        Assert.NotNull(reservation);
        Assert.Equal(ReservationStatus.Accepted, reservation.Status);
        Assert.Equal("Léa", reservation.AcceptedBy);
        Assert.NotNull(reservation.AcceptedAt);
    }

    [Fact]
    public async Task Accept_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Mediator.Send(new AcceptReservationCommand(Guid.NewGuid(), "Léa")));
    }

    [Fact]
    public async Task Confirm_AcceptedReservation_MovesToConfirmed()
    {
        var id = await SeedReservationAsync();
        await Mediator.Send(new AcceptReservationCommand(id, "Léa"));

        await Mediator.Send(new ConfirmReservationCommand(id, "Jean"));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var reservation = await repo.GetByIdAsync(id);
        Assert.NotNull(reservation);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal("Jean", reservation.ConfirmedBy);
    }

    [Fact]
    public async Task Confirm_PendingReservation_Throws()
    {
        var id = await SeedReservationAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Mediator.Send(new ConfirmReservationCommand(id, "Jean")));
    }

    [Fact]
    public async Task Delete_RemovesReservation()
    {
        var id = await SeedReservationAsync();

        await Mediator.Send(new DeleteReservationCommand(id));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var reservation = await repo.GetByIdAsync(id);
        Assert.Null(reservation);
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Mediator.Send(new DeleteReservationCommand(Guid.NewGuid())));
    }
}
