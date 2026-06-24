using Microsoft.EntityFrameworkCore.Storage;
using EcbatanLocation.Application.Messaging;

namespace EcbatanLocation.Infrastructure.Persistence;

public class UnitOfWork(EcbatanLocationDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            _transaction = null;
        }
    }
}
