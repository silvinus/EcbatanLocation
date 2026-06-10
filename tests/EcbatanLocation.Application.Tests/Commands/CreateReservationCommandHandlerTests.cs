using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Commands;

public class CreateReservationCommandHandlerTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidRequest_PersistsPendingReservation()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");
        var cmd = new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 1)]);

        var id = await Mediator.Send(cmd);

        var repo = Services.GetRequiredService<IReservationRepository>();
        var reservation = await repo.GetByIdAsync(id);
        Assert.NotNull(reservation);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal(villa.Id, reservation.StudioId);
        Assert.Equal(owner.Id, reservation.OwnerId);
        Assert.Equal(3, reservation.TotalPersonCount);
    }

    [Fact]
    public async Task Handle_StudioNotFound_Throws()
    {
        var owner = await GetOwnerAsync("Léa");
        var cmd = new CreateReservationCommand(Guid.NewGuid(), owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 1, 0)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Mediator.Send(cmd));
    }

    [Fact]
    public async Task Handle_OverlappingDates_ThrowsOverlappingReservation()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 12),
            "Martin", [new PersonLineDto(ClientType.Owner, 1, 0)]));

        var cmd = new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 15),
            "Dupont", [new PersonLineDto(ClientType.Owner, 1, 0)]);

        await Assert.ThrowsAsync<OverlappingReservationException>(() => Mediator.Send(cmd));
    }

    [Fact]
    public async Task Handle_NonRentableAloneStudio_WithoutIndependentStudio_Throws()
    {
        var centre = await GetStudioAsync("Studio Centre");
        var owner = await GetOwnerAsync("Léa");
        var cmd = new CreateReservationCommand(centre.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Mediator.Send(cmd));
    }

    [Fact]
    public async Task Handle_NonRentableAloneStudio_WithOverlappingIndependentStudio_Succeeds()
    {
        var villa = await GetStudioAsync("Villa");
        var centre = await GetStudioAsync("Studio Centre");
        var owner = await GetOwnerAsync("Léa");

        await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10),
            "Dupont", [new PersonLineDto(ClientType.Owner, 1, 0)]));

        var id = await Mediator.Send(new CreateReservationCommand(centre.Id, owner.Id,
            new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 6),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var reservation = await repo.GetByIdAsync(id);
        Assert.NotNull(reservation);
        Assert.Equal(centre.Id, reservation.StudioId);
    }
}
