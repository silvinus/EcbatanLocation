using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.Commands.UpdateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Commands;

public class UpdateReservationCommandHandlerTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ReservationNotFound_Throws()
    {
        var villa = await GetStudioAsync("Villa");
        var cmd = new UpdateReservationCommand(Guid.NewGuid(), villa.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8), "Dupont",
            [new PersonLineDto(ClientType.Owner, 1, 0)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Mediator.Send(cmd));
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesDatesAndTenant()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");
        var id = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        await Mediator.Send(new UpdateReservationCommand(id, villa.Id,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5), "Martin",
            [new PersonLineDto(ClientType.GuestWithPresence, 3, 0)]));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var reservation = await repo.GetByIdAsync(id);
        Assert.NotNull(reservation);
        Assert.Equal("Martin", reservation.TenantName);
        Assert.Equal(new DateOnly(2026, 8, 1), reservation.Dates.StartDate);
        Assert.Equal(3, reservation.TotalPersonCount);
    }

    [Fact]
    public async Task Handle_KeepingOwnDates_DoesNotFlagSelfAsOverlap()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");
        var id = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        await Mediator.Send(new UpdateReservationCommand(id, villa.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8), "Dupont",
            [new PersonLineDto(ClientType.Owner, 2, 0)]));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var reservation = await repo.GetByIdAsync(id);
        Assert.NotNull(reservation);
    }

    [Fact]
    public async Task Handle_OverlappingAnotherReservation_Throws()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        var toUpdateId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]));

        var cmd = new UpdateReservationCommand(toUpdateId, villa.Id,
            new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 10), "Dupont",
            [new PersonLineDto(ClientType.Owner, 2, 0)]);

        await Assert.ThrowsAsync<OverlappingReservationException>(() => Mediator.Send(cmd));
    }
}
