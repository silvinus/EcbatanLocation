using EcbatanLocation.Application.Commands.AcceptReservation;
using EcbatanLocation.Application.Commands.ConfirmReservation;
using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.Commands.DeleteReservation;
using EcbatanLocation.Application.Commands.UpdateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Commands;

/// <summary>
/// A command acting on an existing reservation (accept / confirm / update / delete) may only be
/// performed by the reservation's owner or by an administrator. The base fixture signs in as Léa,
/// so reservations created here belong to Léa unless stated otherwise.
/// </summary>
public class ReservationOwnershipTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateOnly Start = new(2026, 8, 3);
    private static readonly DateOnly End = new(2026, 8, 10);

    private async Task<Guid> CreateLeaReservationAsync()
    {
        var villa = await GetStudioAsync("Villa");
        var lea = await GetOwnerAsync("Léa");
        return await Mediator.Send(new CreateReservationCommand(villa.Id, lea.Id, Start, End,
            "Tenant", [new PersonLineDto(ClientType.Owner, 2, 0)]));
    }

    private async Task ActAsAsync(string ownerName)
    {
        var owner = await GetOwnerAsync(ownerName);
        AuthState.SetOwner(owner.UserId, owner.Name);
    }

    [Fact]
    public async Task Owner_CanAccept_OwnReservation()
    {
        var id = await CreateLeaReservationAsync();

        await Mediator.Send(new AcceptReservationCommand(id, "Léa"));

        var repo = Services.GetRequiredService<IReservationRepository>();
        Assert.Equal(ReservationStatus.Accepted, (await repo.GetByIdAsync(id))!.Status);
    }

    [Fact]
    public async Task NonOwner_CannotAccept_OthersReservation()
    {
        var id = await CreateLeaReservationAsync();
        await ActAsAsync("Sarah");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Mediator.Send(new AcceptReservationCommand(id, "Sarah")));
    }

    [Fact]
    public async Task Admin_CanAccept_OthersReservation()
    {
        var id = await CreateLeaReservationAsync();
        AuthState.SetAdmin();

        await Mediator.Send(new AcceptReservationCommand(id, "Admin"));

        var repo = Services.GetRequiredService<IReservationRepository>();
        Assert.Equal(ReservationStatus.Accepted, (await repo.GetByIdAsync(id))!.Status);
    }

    [Fact]
    public async Task NonOwner_CannotConfirm_OthersReservation()
    {
        var id = await CreateLeaReservationAsync();
        await Mediator.Send(new AcceptReservationCommand(id, "Léa"));
        await ActAsAsync("Sarah");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Mediator.Send(new ConfirmReservationCommand(id, "Sarah")));
    }

    [Fact]
    public async Task NonOwner_CannotUpdate_OthersReservation()
    {
        var id = await CreateLeaReservationAsync();
        var villa = await GetStudioAsync("Villa");
        await ActAsAsync("Sarah");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Mediator.Send(new UpdateReservationCommand(id, villa.Id, Start, End,
                "Hacked", [new PersonLineDto(ClientType.Owner, 2, 0)])));
    }

    [Fact]
    public async Task NonOwner_CannotDelete_OthersReservation()
    {
        var id = await CreateLeaReservationAsync();
        await ActAsAsync("Sarah");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Mediator.Send(new DeleteReservationCommand(id)));

        var repo = Services.GetRequiredService<IReservationRepository>();
        Assert.NotNull(await repo.GetByIdAsync(id));
    }

    [Fact]
    public async Task Admin_CanDelete_OthersReservation()
    {
        var id = await CreateLeaReservationAsync();
        AuthState.SetAdmin();

        await Mediator.Send(new DeleteReservationCommand(id));

        var repo = Services.GetRequiredService<IReservationRepository>();
        Assert.Null(await repo.GetByIdAsync(id));
    }

    [Fact]
    public async Task Anonymous_CannotDelete_Reservation()
    {
        var id = await CreateLeaReservationAsync();
        AuthState.SetAnonymous();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Mediator.Send(new DeleteReservationCommand(id)));
    }
}
