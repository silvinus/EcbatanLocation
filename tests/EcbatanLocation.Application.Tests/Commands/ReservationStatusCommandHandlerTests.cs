using EcbatanLocation.Application.Commands.AcceptReservation;
using EcbatanLocation.Application.Commands.ConfirmReservation;
using EcbatanLocation.Application.Commands.DeleteReservation;
using EcbatanLocation.Application.Tests.Fakes;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Tests.Commands;

public class ReservationStatusCommandHandlerTests
{
    private readonly FakeReservationRepository _reservations = new();
    private static readonly Studio Villa = Studio.Create("Villa", 6, true, true, 1);

    private Reservation Seed()
    {
        var r = Reservation.Create(Villa.Id, Guid.NewGuid(),
            new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8)),
            "Dupont", [new PersonLine(ClientType.Owner, 2, 0)], Villa.Capacity);
        _reservations.Items.Add(r);
        return r;
    }

    [Fact]
    public async Task Accept_PendingReservation_MovesToAccepted()
    {
        var r = Seed();
        var handler = new AcceptReservationCommandHandler(_reservations);

        await handler.Handle(new AcceptReservationCommand(r.Id, "Léa"), default);

        Assert.Equal(ReservationStatus.Accepted, r.Status);
        Assert.Equal("Léa", r.AcceptedBy);
        Assert.NotNull(r.AcceptedAt);
    }

    [Fact]
    public async Task Accept_NotFound_Throws()
    {
        var handler = new AcceptReservationCommandHandler(_reservations);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new AcceptReservationCommand(Guid.NewGuid(), "Léa"), default));
    }

    [Fact]
    public async Task Confirm_AcceptedReservation_MovesToConfirmed()
    {
        var r = Seed();
        r.Accept("Léa");
        var handler = new ConfirmReservationCommandHandler(_reservations);

        await handler.Handle(new ConfirmReservationCommand(r.Id, "Jean"), default);

        Assert.Equal(ReservationStatus.Confirmed, r.Status);
        Assert.Equal("Jean", r.ConfirmedBy);
    }

    [Fact]
    public async Task Confirm_PendingReservation_Throws()
    {
        var r = Seed();
        var handler = new ConfirmReservationCommandHandler(_reservations);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new ConfirmReservationCommand(r.Id, "Jean"), default));
    }

    [Fact]
    public async Task Delete_RemovesReservation()
    {
        var r = Seed();
        var handler = new DeleteReservationCommandHandler(_reservations);

        await handler.Handle(new DeleteReservationCommand(r.Id), default);

        Assert.Empty(_reservations.Items);
    }

    [Fact]
    public async Task Delete_NotFound_Throws()
    {
        var handler = new DeleteReservationCommandHandler(_reservations);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteReservationCommand(Guid.NewGuid()), default));
    }
}
