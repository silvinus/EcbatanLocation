using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Tests.Fakes;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Tests.Commands;

public class CreateReservationCommandHandlerTests
{
    private readonly FakeReservationRepository _reservations = new();
    private readonly ReservationDomainService _domainService = new();

    private static readonly Studio Villa = Studio.Create("Villa", 6, true, rentableAlone: true, 1);
    private static readonly Studio Centre = Studio.Create("Studio Centre", 2, false, rentableAlone: false, 4);

    private CreateReservationCommandHandler CreateHandler(params Studio[] studios)
        => new(_reservations, new FakeStudioRepository(studios), _domainService);

    private static CreateReservationCommand Command(Guid studioId, Guid ownerId,
        DateOnly start, DateOnly end, params PersonLineDto[] lines)
        => new(studioId, ownerId, start, end, "Dupont", lines);

    [Fact]
    public async Task Handle_ValidRequest_PersistsPendingReservation()
    {
        var handler = CreateHandler(Villa);
        var ownerId = Guid.NewGuid();
        var cmd = Command(Villa.Id, ownerId,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            new PersonLineDto(ClientType.Owner, 2, 1));

        var id = await handler.Handle(cmd, default);

        var reservation = Assert.Single(_reservations.Items);
        Assert.Equal(id, reservation.Id);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal(Villa.Id, reservation.StudioId);
        Assert.Equal(ownerId, reservation.OwnerId);
        Assert.Equal(3, reservation.TotalPersonCount);
    }

    [Fact]
    public async Task Handle_StudioNotFound_Throws()
    {
        var handler = CreateHandler(Villa);
        var cmd = Command(Guid.NewGuid(), Guid.NewGuid(),
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            new PersonLineDto(ClientType.Owner, 1, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Handle_OverlappingDates_ThrowsOverlappingReservation()
    {
        var existing = Reservation.Create(Villa.Id, Guid.NewGuid(),
            new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 12)),
            "Martin", [new PersonLine(ClientType.Owner, 1, 0)], Villa.Capacity);
        _reservations.Items.Add(existing);

        var handler = CreateHandler(Villa);
        var cmd = Command(Villa.Id, Guid.NewGuid(),
            new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 15),
            new PersonLineDto(ClientType.Owner, 1, 0));

        await Assert.ThrowsAsync<OverlappingReservationException>(() => handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Handle_NonRentableAloneStudio_WithoutIndependentStudio_Throws()
    {
        var handler = CreateHandler(Centre);
        var cmd = Command(Centre.Id, Guid.NewGuid(),
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            new PersonLineDto(ClientType.Owner, 2, 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(cmd, default));
    }

    [Fact]
    public async Task Handle_NonRentableAloneStudio_WithOverlappingIndependentStudio_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        // Same owner already holds an overlapping reservation on an independent studio (Villa).
        _reservations.Items.Add(Reservation.Create(Villa.Id, ownerId,
            new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10)),
            "Dupont", [new PersonLine(ClientType.Owner, 1, 0)], Villa.Capacity));

        var handler = CreateHandler(Villa, Centre);
        var cmd = Command(Centre.Id, ownerId,
            new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 6),
            new PersonLineDto(ClientType.Owner, 2, 0));

        var id = await handler.Handle(cmd, default);

        Assert.Contains(_reservations.Items, r => r.Id == id && r.StudioId == Centre.Id);
    }
}
