using EcbatanLocation.Domain.Common;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Events;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Entities;

public class Reservation : IHasDomainEvents
{
    public Guid Id { get; private set; }
    public Guid StudioId { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateRange Dates { get; private set; } = default!;
    public string TenantName { get; private set; } = default!;
    private List<PersonLine> _personLines = [];
    public IReadOnlyCollection<PersonLine> PersonLines => _personLines.AsReadOnly();
    public ReservationStatus Status { get; private set; }
    public string? AcceptedBy { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Reservation() { }

    public static Reservation Create(
        Guid studioId,
        Guid ownerId,
        DateRange dates,
        string tenantName,
        IEnumerable<PersonLine> personLines,
        int studioCapacity)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
            throw new ArgumentException("Tenant name is required.");

        var lines = personLines.ToList();
        ValidatePersonLines(lines, studioCapacity);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            StudioId = studioId,
            OwnerId = ownerId,
            Dates = dates,
            TenantName = tenantName,
            _personLines = lines,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        reservation._domainEvents.Add(new ReservationCreated(reservation.Id, studioId, ownerId));
        return reservation;
    }

    public void Accept(string by)
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only a 'Pending' reservation can be accepted.");

        Status = ReservationStatus.Accepted;
        AcceptedBy = by;
        AcceptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ReservationAccepted(Id, by));
    }

    public void Confirm(string by)
    {
        if (Status != ReservationStatus.Accepted)
            throw new InvalidOperationException("Only an 'Accepted' reservation can be confirmed.");

        Status = ReservationStatus.Confirmed;
        ConfirmedBy = by;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ReservationConfirmed(Id, by));
    }

    /// <summary>Records the deletion event. Call before removing the reservation from its repository.</summary>
    public void MarkDeleted()
    {
        _domainEvents.Add(new ReservationDeleted(Id));
    }

    public void Update(
        DateRange dates,
        string tenantName,
        IEnumerable<PersonLine> personLines,
        int studioCapacity)
    {
        var lines = personLines.ToList();
        ValidatePersonLines(lines, studioCapacity);

        Dates = dates;
        TenantName = tenantName;
        _personLines = lines;
        UpdatedAt = DateTime.UtcNow;
    }

    public int TotalPersonCount => _personLines.Sum(l => l.TotalPersons);
    public int TotalAdultCount => _personLines.Sum(l => l.AdultCount);
    public int TotalChildrenUnder3Count => _personLines.Sum(l => l.ChildrenUnder3Count);

    private static void ValidatePersonLines(List<PersonLine> lines, int studioCapacity)
    {
        if (lines.Count == 0)
            throw new ArgumentException("At least one person line is required.");

        if (lines.Any(l => l.AdultCount < 0))
            throw new ArgumentException("Adult count cannot be negative.");
        if (lines.Any(l => l.ChildrenUnder3Count < 0))
            throw new ArgumentException("Children count cannot be negative.");

        var totalAdults = lines.Sum(l => l.AdultCount);
        if (totalAdults < 1)
            throw new ArgumentException("At least one adult is required.");

        if (totalAdults > studioCapacity)
            throw new InvalidOperationException(
                $"Capacity exceeded: {totalAdults} adults for a capacity of {studioCapacity}.");
    }
}
