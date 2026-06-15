using Microsoft.EntityFrameworkCore;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;
using EcbatanLocation.Infrastructure.Persistence;

namespace EcbatanLocation.Infrastructure.Repositories;

public class ReservationRepository(EcbatanLocationDbContext context) : IReservationRepository
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
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        await GuardNoOverlapAsync(reservation, ct);
        await context.Reservations.AddAsync(reservation, ct);
        await SaveTranslatingOverlapAsync(ct);

        await tx.CommitAsync(ct);
    }

    public async Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        await GuardNoOverlapAsync(reservation, ct);
        context.Reservations.Update(reservation);
        await SaveTranslatingOverlapAsync(ct);

        await tx.CommitAsync(ct);
    }

    private async Task GuardNoOverlapAsync(Reservation reservation, CancellationToken ct)
    {
        var overlap = await context.Reservations
            .Where(r => r.StudioId == reservation.StudioId && r.Id != reservation.Id)
            .AnyAsync(
                r => r.Dates.StartDate < reservation.Dates.EndDate && r.Dates.EndDate > reservation.Dates.StartDate,
                ct);

        if (overlap)
            throw new OverlappingReservationException();
    }

    private async Task SaveTranslatingOverlapAsync(CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueOverlapViolation(ex))
        {
            throw new OverlappingReservationException();
        }
    }

    private static bool IsUniqueOverlapViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("IX_Reservations_StudioId_StartDate_EndDate") == true;

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

    public async Task<bool> ExistsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await context.Reservations.AnyAsync(r => r.OwnerId == ownerId, ct);
    }

    public async Task<IReadOnlySet<Guid>> GetOwnerIdsWithReservationsAsync(CancellationToken ct = default)
    {
        var ids = await context.Reservations
            .Select(r => r.OwnerId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<bool> ExistsByStudioAsync(Guid studioId, CancellationToken ct = default)
    {
        return await context.Reservations.AnyAsync(r => r.StudioId == studioId, ct);
    }

    public async Task<IReadOnlySet<Guid>> GetStudioIdsWithReservationsAsync(CancellationToken ct = default)
    {
        var ids = await context.Reservations
            .Select(r => r.StudioId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<IReadOnlyList<Reservation>> GetByYearAsync(int year, CancellationToken ct = default)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year + 1, 1, 1);

        return await context.Reservations
            .Where(r => r.Dates.StartDate < yearEnd && r.Dates.EndDate > yearStart)
            .ToListAsync(ct);
    }
}
