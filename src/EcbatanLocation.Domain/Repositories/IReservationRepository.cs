using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByMonthAsync(int year, int month, CancellationToken ct = default);
    Task<bool> ExistsOverlapAsync(Guid studioId, DateRange dates, Guid? excludeReservationId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetOverlappingByStudioAsync(Guid studioId, DateRange dates, Guid? excludeReservationId = null, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetHypotheticalsByStudioAsync(Guid studioId, DateRange dates, CancellationToken ct = default);
    Task<int> BackfillBedCountForStudioAsync(Guid studioId, int numberOfBeds, CancellationToken ct = default);
    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByOwnerAndOverlappingDatesAsync(Guid ownerId, DateRange dates, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByDateAsync(DateOnly date, CancellationToken ct = default);
    Task<bool> ExistsByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlySet<Guid>> GetOwnerIdsWithReservationsAsync(CancellationToken ct = default);
    Task<bool> ExistsByStudioAsync(Guid studioId, CancellationToken ct = default);
    Task<IReadOnlySet<Guid>> GetStudioIdsWithReservationsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByYearAsync(int year, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetDependentsByParentIdAsync(Guid parentId, CancellationToken ct = default);
    Task<bool> HasDependentsAsync(Guid reservationId, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetCompatibleParentsAsync(Guid ownerId, DateRange dates, Guid? excludeId = null, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task UpdateRangeAsync(IReadOnlyList<Reservation> reservations, CancellationToken ct = default);
}
