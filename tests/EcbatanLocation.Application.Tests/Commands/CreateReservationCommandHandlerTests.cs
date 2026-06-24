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
    public async Task Handle_NonRentableAloneStudio_WithoutParent_Throws()
    {
        var centre = await GetStudioAsync("Studio Centre");
        var owner = await GetOwnerAsync("Léa");
        var cmd = new CreateReservationCommand(centre.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Mediator.Send(cmd));
    }

    [Fact]
    public async Task Handle_UnavailableStudio_Throws()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");

        villa.Update(villa.Name, villa.Capacity, villa.HasKitchen, villa.RentableAlone, unavailable: true);
        var studioRepo = Services.GetRequiredService<IStudioRepository>();
        await studioRepo.UpdateAsync(villa);

        var cmd = new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 1, 0)]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Mediator.Send(cmd));
        Assert.Contains("unavailable", ex.Message);
    }

    [Fact]
    public async Task Handle_NonRentableAlone_WithParent_Succeeds()
    {
        var villa = await GetStudioAsync("Villa");
        var centre = await GetStudioAsync("Studio Centre");
        var owner = await GetOwnerAsync("Léa");

        var parentId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10),
            "Dupont", [new PersonLineDto(ClientType.Owner, 1, 0)]));

        var childId = await Mediator.Send(new CreateReservationCommand(centre.Id, owner.Id,
            new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 6),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)],
            ParentReservationId: parentId));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var child = await repo.GetByIdAsync(childId);
        Assert.NotNull(child);
        Assert.Equal(centre.Id, child.StudioId);
        Assert.Equal(parentId, child.ParentReservationId);
    }

    [Fact]
    public async Task Handle_WithParent_InheritsParentStatus()
    {
        var villa = await GetStudioAsync("Villa");
        var centre = await GetStudioAsync("Studio Centre");
        var owner = await GetOwnerAsync("Léa");

        var parentId = await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10),
            "Dupont", [new PersonLineDto(ClientType.Owner, 1, 0)]));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var parent = await repo.GetByIdAsync(parentId);
        parent!.Accept("Jean");
        await repo.UpdateAsync(parent);

        var childId = await Mediator.Send(new CreateReservationCommand(centre.Id, owner.Id,
            new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 6),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)],
            ParentReservationId: parentId));

        var child = await repo.GetByIdAsync(childId);
        Assert.NotNull(child);
        Assert.Equal(ReservationStatus.Accepted, child.Status);
        Assert.Equal("Jean", child.AcceptedBy);
    }

    [Fact]
    public async Task Handle_NonRentableAlone_ParentNotFound_Throws()
    {
        var centre = await GetStudioAsync("Studio Centre");
        var owner = await GetOwnerAsync("Léa");

        var cmd = new CreateReservationCommand(centre.Id, owner.Id,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 0)],
            ParentReservationId: Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Mediator.Send(cmd));
    }
}
