using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Application.Tests;

public class IntegrationTestFixture : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private IServiceScope _scope = null!;
    private string _dbPath = null!;

    public IServiceProvider Services => _scope.ServiceProvider;
    public TestAuthenticationStateProvider AuthState { get; } = new();

    public async Task InitializeAsync()
    {
        _dbPath = $"integration_test_{Guid.NewGuid():N}.db";

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
                builder.ConfigureServices(services =>
                {
                    var dbDescriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<EcbatanLocationDbContext>)
                                 || d.ServiceType == typeof(DbContextOptions))
                        .ToList();
                    foreach (var d in dbDescriptors)
                        services.Remove(d);

                    services.AddDbContext<EcbatanLocationDbContext>((sp, options) =>
                        options.UseSqlite($"Data Source={_dbPath}")
                               .AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>()));

                    var authDescriptors = services
                        .Where(d => d.ServiceType == typeof(AuthenticationStateProvider))
                        .ToList();
                    foreach (var d in authDescriptors)
                        services.Remove(d);
                    services.AddScoped<AuthenticationStateProvider>(_ => AuthState);
                });
            });

        _scope = _factory.Services.CreateScope();
        await DbInitializer.InitializeAsync(_factory.Services);
    }

    public IServiceScope CreateScope() => _factory.Services.CreateScope();

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await _factory.DisposeAsync();

        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
