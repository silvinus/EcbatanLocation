using Microsoft.EntityFrameworkCore;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Infrastructure.Repositories;

public class OwnerRepository(EcbatanLocationDbContext context) : IOwnerRepository
{
    public async Task<Owner?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Owners.FindAsync([id], ct);

    public async Task<Owner?> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => await context.Owners.FirstOrDefaultAsync(o => o.UserId == userId, ct);

    public async Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken ct = default)
        => await context.Owners.OrderBy(o => o.Name).ToListAsync(ct);

    public async Task AddAsync(Owner owner, CancellationToken ct = default)
    {
        await context.Owners.AddAsync(owner, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Owner owner, CancellationToken ct = default)
    {
        context.Owners.Update(owner);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Owner owner, CancellationToken ct = default)
    {
        context.Owners.Remove(owner);
        await context.SaveChangesAsync(ct);
    }
}
