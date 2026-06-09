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

    public async Task<IReadOnlyList<Reservation>> GetByMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        return await context.Reservations
            .Where(r => r.Dates.StartDate < monthEnd && r.Dates.EndDate > monthStart)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsOverlapAsync(
        Guid studioId,
        DateRange dates,
        Guid? excludeReservationId = null,
        CancellationToken ct = default)
    {
        var query = context.Reservations
            .Where(r => r.StudioId == studioId)
            .Where(r => r.Dates.StartDate < dates.EndDate && r.Dates.EndDate > dates.StartDate);

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

    public async Task<IReadOnlyList<Reservation>> GetByOwnerAndOverlappingDatesAsync(
        Guid ownerId, DateRange dates, CancellationToken ct = default)
    {
        return await context.Reservations
            .Where(r => r.OwnerId == ownerId)
            .Where(r => r.Dates.StartDate < dates.EndDate && r.Dates.EndDate > dates.StartDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Reservation>> GetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        return await context.Reservations
            .Where(r => r.Dates.StartDate <= date && r.Dates.EndDate > date)
            .ToListAsync(ct);
    }
}
