using EcbatanLocation.Domain.Entities;

namespace EcbatanLocation.Domain.Repositories;

public interface IPricingGridRepository
{
    Task<PricingGrid?> GetByYearAsync(int year, CancellationToken ct = default);
    Task AddAsync(PricingGrid grid, CancellationToken ct = default);
    Task UpdateAsync(PricingGrid grid, CancellationToken ct = default);
}
