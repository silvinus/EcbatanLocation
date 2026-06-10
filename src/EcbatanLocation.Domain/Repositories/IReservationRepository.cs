using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByMonthAsync(int year, int month, CancellationToken ct = default);
    Task<bool> ExistsOverlapAsync(Guid studioId, DateRange dates, Guid? excludeReservationId = null, CancellationToken ct = default);
    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByOwnerAndOverlappingDatesAsync(Guid ownerId, DateRange dates, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByDateAsync(DateOnly date, CancellationToken ct = default);
}
