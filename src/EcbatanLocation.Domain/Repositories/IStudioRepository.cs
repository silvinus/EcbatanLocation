using EcbatanLocation.Domain.Entities;

namespace EcbatanLocation.Domain.Repositories;

public interface IStudioRepository
{
    Task<Studio?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Studio>> GetAllAsync(CancellationToken ct = default);
}
