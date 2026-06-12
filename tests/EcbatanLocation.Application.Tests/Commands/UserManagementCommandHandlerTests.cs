using EcbatanLocation.Application.Commands.CreateUser;
using EcbatanLocation.Application.Commands.DeleteUser;
using EcbatanLocation.Application.Commands.ResetPassword;
using EcbatanLocation.Application.Commands.UpdateUser;
using EcbatanLocation.Application.Queries.GetUsers;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Commands;

public class UserManagementCommandHandlerTests(IntegrationTestFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task CreateUser_Owner_CreatesUserAndOwnerEntry()
    {
        AuthState.SetAdmin();

        var result = await Mediator.Send(
            new CreateUserCommand("Pierre", "pierre@test.fr", ["Owner"]));

        Assert.NotNull(result.UserId);
        Assert.NotEmpty(result.GeneratedPassword);

        var ownerRepo = Services.GetRequiredService<IOwnerRepository>();
        var owner = await ownerRepo.GetByUserIdAsync(result.UserId);
        Assert.NotNull(owner);
        Assert.Equal("Pierre", owner.Name);
    }

    [Fact]
    public async Task CreateUser_Admin_NoOwnerEntry()
    {
        AuthState.SetAdmin();

        var result = await Mediator.Send(
            new CreateUserCommand("AdminTest", "admintest@test.fr", ["Admin"]));

        var ownerRepo = Services.GetRequiredService<IOwnerRepository>();
        var owner = await ownerRepo.GetByUserIdAsync(result.UserId);
        Assert.Null(owner);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Throws()
    {
        AuthState.SetAdmin();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Mediator.Send(new CreateUserCommand("Dup", "lea@EcbatanLocation.fr", ["Owner"])));
    }

    [Fact]
    public async Task CreateUser_RequiresAdmin_ThrowsForOwner()
    {
        AuthState.SetOwner();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Mediator.Send(new CreateUserCommand("Test", "new@test.fr", ["Owner"])));
    }

    [Fact]
    public async Task UpdateUser_ChangesNameAndRoles()
    {
        AuthState.SetAdmin();

        var created = await Mediator.Send(
            new CreateUserCommand("Temp", "temp-update@test.fr", ["Owner"]));

        await Mediator.Send(
            new UpdateUserCommand(created.UserId, "TempRenamed", "temp-update@test.fr", ["Owner", "Admin"]));

        var userService = Services.GetRequiredService<IUserService>();
        var updated = await userService.GetByIdAsync(created.UserId);
        Assert.NotNull(updated);
        Assert.Equal("TempRenamed", updated.DisplayName);
        Assert.Contains("Admin", updated.Roles);

        var ownerRepo = Services.GetRequiredService<IOwnerRepository>();
        var owner = await ownerRepo.GetByUserIdAsync(created.UserId);
        Assert.NotNull(owner);
        Assert.Equal("TempRenamed", owner.Name);
    }

    [Fact]
    public async Task UpdateUser_RemoveOwnerRole_DeletesOwnerEntry()
    {
        AuthState.SetAdmin();

        var created = await Mediator.Send(
            new CreateUserCommand("OwnerToAdmin", "ota@test.fr", ["Owner"]));

        await Mediator.Send(
            new UpdateUserCommand(created.UserId, "OwnerToAdmin", "ota@test.fr", ["Admin"]));

        var ownerRepo = Services.GetRequiredService<IOwnerRepository>();
        var owner = await ownerRepo.GetByUserIdAsync(created.UserId);
        Assert.Null(owner);
    }

    [Fact]
    public async Task DeleteUser_WithReservations_Throws()
    {
        AuthState.SetAdmin();

        var owner = await GetOwnerAsync("Léa");
        var studio = await GetStudioAsync("Villa");
        var dates = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5));

        var reservationRepo = Services.GetRequiredService<IReservationRepository>();
        var reservation = Reservation.Create(
            studio.Id, owner.Id, dates, "Test",
            [new Domain.ValueObjects.PersonLine(Domain.Enums.ClientType.Owner, 2, 0)],
            studio.Capacity);
        await reservationRepo.AddAsync(reservation);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Mediator.Send(new DeleteUserCommand(owner.UserId)));
    }

    [Fact]
    public async Task DeleteUser_NoReservations_Succeeds()
    {
        AuthState.SetAdmin();

        var created = await Mediator.Send(
            new CreateUserCommand("ToDelete", "todelete@test.fr", ["Owner"]));

        await Mediator.Send(new DeleteUserCommand(created.UserId));

        var userService = Services.GetRequiredService<IUserService>();
        var deleted = await userService.GetByIdAsync(created.UserId);
        Assert.Null(deleted);

        var ownerRepo = Services.GetRequiredService<IOwnerRepository>();
        var owner = await ownerRepo.GetByUserIdAsync(created.UserId);
        Assert.Null(owner);
    }

    [Fact]
    public async Task ResetPassword_ReturnsNewPassword()
    {
        AuthState.SetAdmin();

        var created = await Mediator.Send(
            new CreateUserCommand("PwdTest", "pwdtest@test.fr", ["Owner"]));

        var newPassword = await Mediator.Send(new ResetPasswordCommand(created.UserId));

        Assert.NotEmpty(newPassword);
        Assert.True(newPassword.Length >= 12);
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsersWithRoles()
    {
        AuthState.SetAdmin();

        var users = await Mediator.Send(new GetUsersQuery());

        Assert.True(users.Count >= 5); // 4 owners + Sylvain admin
        Assert.Contains(users, u => u.DisplayName == "Christophe" && u.Roles.Contains("Admin") && u.Roles.Contains("Owner"));
        Assert.Contains(users, u => u.DisplayName == "Sylvain" && u.Roles.Contains("Admin"));
    }
}
