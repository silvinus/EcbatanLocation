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

    private async Task<(Guid ParentId, Guid ChildId)> SeedParentChildAsync()
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

        return (parentId, childId);
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
    public async Task Accept_PropagatesStatusToDependents()
    {
        var (parentId, childId) = await SeedParentChildAsync();

        await Mediator.Send(new AcceptReservationCommand(parentId, "Jean"));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var child = await repo.GetByIdAsync(childId);
        Assert.NotNull(child);
        Assert.Equal(ReservationStatus.Accepted, child.Status);
        Assert.Equal("Jean", child.AcceptedBy);
    }

    [Fact]
    public async Task Accept_DependentReservation_Throws()
    {
        var (_, childId) = await SeedParentChildAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Mediator.Send(new AcceptReservationCommand(childId, "Jean")));
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
    public async Task Confirm_PropagatesStatusToDependents()
    {
        var (parentId, childId) = await SeedParentChildAsync();
        await Mediator.Send(new AcceptReservationCommand(parentId, "Jean"));

        await Mediator.Send(new ConfirmReservationCommand(parentId, "Léa"));

        var repo = Services.GetRequiredService<IReservationRepository>();
        var child = await repo.GetByIdAsync(childId);
        Assert.NotNull(child);
        Assert.Equal(ReservationStatus.Confirmed, child.Status);
        Assert.Equal("Léa", child.ConfirmedBy);
    }

    [Fact]
    public async Task Confirm_DependentReservation_Throws()
    {
        var (parentId, childId) = await SeedParentChildAsync();
        await Mediator.Send(new AcceptReservationCommand(parentId, "Jean"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Mediator.Send(new ConfirmReservationCommand(childId, "Jean")));
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

    [Fact]
    public async Task Delete_CascadesToDependents()
    {
        var (parentId, childId) = await SeedParentChildAsync();

        await Mediator.Send(new DeleteReservationCommand(parentId));

        var repo = Services.GetRequiredService<IReservationRepository>();
        Assert.Null(await repo.GetByIdAsync(parentId));
        Assert.Null(await repo.GetByIdAsync(childId));
    }
}
