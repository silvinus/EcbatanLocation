using Microsoft.Extensions.DependencyInjection;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Application.Tests;

public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture;
    private IServiceScope _scope = null!;

    protected IServiceProvider Services => _scope.ServiceProvider;
    protected IMediator Mediator => Services.GetRequiredService<IMediator>();
    protected EcbatanLocationDbContext Db => Services.GetRequiredService<EcbatanLocationDbContext>();
    protected TestAuthenticationStateProvider AuthState => Fixture.AuthState;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _scope = Fixture.CreateScope();
        // Default identity: a real seeded owner (Léa), so reservation tests act as the owner of the
        // reservations they create and pass the ownership authorization check. Tests that need a
        // different actor override this with AuthState.SetOwner(...)/SetAdmin()/SetAnonymous().
        var lea = await GetOwnerAsync("Léa");
        AuthState.SetOwner(lea.UserId, lea.Name);
        await CleanReservationsAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<Studio> GetStudioAsync(string name)
    {
        var repo = Services.GetRequiredService<IStudioRepository>();
        var studios = await repo.GetAllAsync();
        return studios.First(s => s.Name == name);
    }

    protected async Task<Owner> GetOwnerAsync(string name)
    {
        var repo = Services.GetRequiredService<IOwnerRepository>();
        var owners = await repo.GetAllAsync();
        return owners.First(o => o.Name == name);
    }

    private async Task CleanReservationsAsync()
    {
        var db = Db;
        db.Reservations.RemoveRange(db.Reservations);
        await db.SaveChangesAsync();
    }
}
