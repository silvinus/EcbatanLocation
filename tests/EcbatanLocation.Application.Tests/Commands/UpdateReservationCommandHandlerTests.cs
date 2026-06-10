using EcbatanLocation.Application.Commands.UpdateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Tests.Fakes;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Tests.Commands;

public class UpdateReservationCommandHandlerTests
{
    private readonly FakeReservationRepository _reservations = new();
    private readonly ReservationDomainService _domainService = new();
    private static readonly Studio Villa = Studio.Create("Villa", 6, true, rentableAlone: true, 1);

    private UpdateReservationCommandHandler CreateHandler()
        => new(_reservations, new FakeStudioRepository(Villa), _domainService);

    private Reservation SeedReservation(DateOnly start, DateOnly end)
    {
        var r = Reservation.Create(Villa.Id, Guid.NewGuid(),
            new DateRange(start, end), "Dupont",
            [new PersonLine(ClientType.Owner, 2, 0)], Villa.Capacity);
        _reservations.Items.Add(r);
        return r;
    }

    [Fact]
    public async Task Handle_ReservationNotFound_Throws()
    {
        var handler = CreateHandler();
        var cmd = new UpdateReservationCommand(Guid.NewGuid(), Villa.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8), "Dupont",
            [new PersonLineDto(ClientType.Owner, 1, 0)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesDatesAndTenant()
    {
        var existing = SeedReservation(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var handler = CreateHandler();
        var cmd = new UpdateReservationCommand(existing.Id, Villa.Id,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5), "Martin",
            [new PersonLineDto(ClientType.GuestWithPresence, 3, 0)]);

        await handler.Handle(cmd, default);

        Assert.Equal("Martin", existing.TenantName);
        Assert.Equal(new DateOnly(2026, 8, 1), existing.Dates.StartDate);
        Assert.Equal(3, existing.TotalPersonCount);
    }

    [Fact]
    public async Task Handle_KeepingOwnDates_DoesNotFlagSelfAsOverlap()
    {
        var existing = SeedReservation(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var handler = CreateHandler();
        // Same dates as itself: the only reservation on the studio is this one → no overlap.
        var cmd = new UpdateReservationCommand(existing.Id, Villa.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8), "Dupont",
            [new PersonLineDto(ClientType.Owner, 2, 0)]);

        await handler.Handle(cmd, default);

        Assert.Single(_reservations.Items);
    }

    [Fact]
    public async Task Handle_OverlappingAnotherReservation_Throws()
    {
        SeedReservation(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var toUpdate = SeedReservation(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5));
        var handler = CreateHandler();
        var cmd = new UpdateReservationCommand(toUpdate.Id, Villa.Id,
            new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 10), "Dupont",
            [new PersonLineDto(ClientType.Owner, 2, 0)]);

        await Assert.ThrowsAsync<OverlappingReservationException>(() => handler.Handle(cmd, default));
    }
}
