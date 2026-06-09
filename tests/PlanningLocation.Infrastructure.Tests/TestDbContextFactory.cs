using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlanningLocation.Infrastructure.Persistence;

namespace PlanningLocation.Infrastructure.Tests;

public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public PlanningLocationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningLocationDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new PlanningLocationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
