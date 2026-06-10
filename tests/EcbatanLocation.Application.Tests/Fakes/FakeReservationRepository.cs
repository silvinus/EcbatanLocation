using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Tests.Fakes;

/// <summary>In-memory <see cref="IReservationRepository"/> for handler tests.</summary>
public sealed class FakeReservationRepository : IReservationRepository
{
    public List<Reservation> Items { get; } = [];

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Items.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Reservation>> GetByMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        IReadOnlyList<Reservation> result =
            Items.Where(r => r.Dates.StartDate < end && r.Dates.EndDate > start).ToList();
        return Task.FromResult(result);
    }

    public Task<bool> ExistsOverlapAsync(
        Guid studioId, DateRange dates, Guid? excludeReservationId = null, CancellationToken ct = default)
    {
        var exists = Items.Any(r => r.StudioId == studioId
            && (excludeReservationId is null || r.Id != excludeReservationId)
            && r.Dates.Overlaps(dates));
        return Task.FromResult(exists);
    }

    public Task AddAsync(Reservation reservation, CancellationToken ct = default)
    {
        Items.Add(reservation);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        if (!Items.Contains(reservation))
        {
            Items.RemoveAll(r => r.Id == reservation.Id);
            Items.Add(reservation);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        Items.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Reservation>> GetByOwnerAndOverlappingDatesAsync(
        Guid ownerId, DateRange dates, CancellationToken ct = default)
    {
        IReadOnlyList<Reservation> result =
            Items.Where(r => r.OwnerId == ownerId && r.Dates.Overlaps(dates)).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Reservation>> GetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        IReadOnlyList<Reservation> result = Items.Where(r => r.Dates.ContainsDay(date)).ToList();
        return Task.FromResult(result);
    }
}
