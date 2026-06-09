using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByMoisAsync(int annee, int mois, CancellationToken ct = default);
    Task<bool> ExisteChevauchementAsync(Guid studioId, DateRange dates, Guid? excludeReservationId = null, CancellationToken ct = default);
    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
