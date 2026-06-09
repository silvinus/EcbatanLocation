using Microsoft.EntityFrameworkCore;
using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Domain.ValueObjects;
using PlanningLocation.Infrastructure.Persistence;

namespace PlanningLocation.Infrastructure.Repositories;

public class ReservationRepository(PlanningLocationDbContext context) : IReservationRepository
{
    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Reservations.FindAsync([id], ct);

    public async Task<IReadOnlyList<Reservation>> GetByMoisAsync(int annee, int mois, CancellationToken ct = default)
    {
        var debutMois = new DateOnly(annee, mois, 1);
        var finMois = debutMois.AddMonths(1);

        return await context.Reservations
            .Where(r => r.Dates.DateDebut < finMois && r.Dates.DateFin > debutMois)
            .ToListAsync(ct);
    }

    public async Task<bool> ExisteChevauchementAsync(
        Guid studioId,
        DateRange dates,
        Guid? excludeReservationId = null,
        CancellationToken ct = default)
    {
        var query = context.Reservations
            .Where(r => r.StudioId == studioId)
            .Where(r => r.Dates.DateDebut < dates.DateFin && r.Dates.DateFin > dates.DateDebut);

        if (excludeReservationId.HasValue)
            query = query.Where(r => r.Id != excludeReservationId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken ct = default)
    {
        await context.Reservations.AddAsync(reservation, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        context.Reservations.Update(reservation);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var reservation = await context.Reservations.FindAsync([id], ct);
        if (reservation is not null)
        {
            context.Reservations.Remove(reservation);
            await context.SaveChangesAsync(ct);
        }
    }
}
