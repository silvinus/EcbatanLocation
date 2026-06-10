using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Infrastructure.Tests;

public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public EcbatanLocationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EcbatanLocationDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new EcbatanLocationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => _connection.Dispose();
}
