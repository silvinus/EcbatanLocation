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

    /// <summary>
    /// Number of beds this reservation occupies on a per-bed studio.
    /// 0 for a whole-lodging reservation (the studio is taken as a whole).
    /// </summary>
    public int BedCount { get; private set; }

    /// <summary>
    /// A hypothetical (tentative) reservation is staked over dates already taken by another
    /// not-yet-confirmed reservation. It bypasses the availability checks, never occupies a
    /// place (it is ignored by overlap and occupation computations) and is locked at
    /// <see cref="ReservationStatus.Pending"/> — it cannot be accepted or confirmed until it is
    /// promoted to a real reservation (see <see cref="PromoteFromHypothetical"/>).
    /// </summary>
    public bool IsHypothetical { get; private set; }

    private List<PersonLine> _personLines = [];
    public IReadOnlyCollection<PersonLine> PersonLines => _personLines.AsReadOnly();
    public ReservationStatus Status { get; private set; }
    public string? AcceptedBy { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public Guid? ParentReservationId { get; private set; }
    public bool HasParent => ParentReservationId.HasValue;
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
        int studioCapacity,
        Enums.RentalMode rentalMode = Enums.RentalMode.PerLodging,
        int studioBeds = 0,
        int bedCount = 0,
        bool isHypothetical = false)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
            throw new ArgumentException("Tenant name is required.");

        var lines = personLines.ToList();
        ValidatePersonLines(lines, studioCapacity);
        var beds = NormalizeBedCount(rentalMode, studioBeds, bedCount);

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            StudioId = studioId,
            OwnerId = ownerId,
            Dates = dates,
            TenantName = tenantName,
            BedCount = beds,
            IsHypothetical = isHypothetical,
            _personLines = lines,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        reservation._domainEvents.Add(new ReservationCreated(reservation.Id, studioId, ownerId));
        return reservation;
    }

    public void Accept(string by)
    {
        if (IsHypothetical)
            throw new InvalidOperationException(
                "A hypothetical reservation cannot be accepted. Promote it to a real reservation first.");

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
        if (IsHypothetical)
            throw new InvalidOperationException(
                "A hypothetical reservation cannot be confirmed. Promote it to a real reservation first.");

        if (Status != ReservationStatus.Accepted)
            throw new InvalidOperationException("Only an 'Accepted' reservation can be confirmed.");

        Status = ReservationStatus.Confirmed;
        ConfirmedBy = by;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ReservationConfirmed(Id, by));
    }

    public void MarkDeleted()
    {
        Status = ReservationStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ReservationDeleted(Id));
    }

    public void Update(
        DateRange dates,
        string tenantName,
        IEnumerable<PersonLine> personLines,
        int studioCapacity,
        Enums.RentalMode rentalMode = Enums.RentalMode.PerLodging,
        int studioBeds = 0,
        int bedCount = 0,
        bool isHypothetical = false)
    {
        var lines = personLines.ToList();
        ValidatePersonLines(lines, studioCapacity);

        Dates = dates;
        TenantName = tenantName;
        BedCount = NormalizeBedCount(rentalMode, studioBeds, bedCount);
        IsHypothetical = isHypothetical;
        _personLines = lines;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Turns a hypothetical reservation into a regular <see cref="ReservationStatus.Pending"/>
    /// reservation. Used when the slot it was staked over frees up (the blocking reservation was
    /// removed or moved) and the hypothetical now fits the studio's availability.
    /// </summary>
    public void PromoteFromHypothetical()
    {
        if (!IsHypothetical)
            return;

        IsHypothetical = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Data backfill for reservations that predate their studio switching to per-bed mode
    /// (their <see cref="BedCount"/> was left at 0). Assigns a bed count without going through
    /// the normal create/update validation flow.
    /// </summary>
    public void BackfillBedCount(int bedCount)
    {
        BedCount = Math.Max(1, bedCount);
    }

    public void SetParentReservation(Guid parentId)
    {
        ParentReservationId = parentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearParentReservation()
    {
        ParentReservationId = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void InheritStatus(
        ReservationStatus status,
        string? acceptedBy,
        DateTime? acceptedAt,
        string? confirmedBy,
        DateTime? confirmedAt)
    {
        Status = status;
        AcceptedBy = acceptedBy;
        AcceptedAt = acceptedAt;
        ConfirmedBy = confirmedBy;
        ConfirmedAt = confirmedAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public int TotalPersonCount => _personLines.Sum(l => l.TotalPersons);
    public int TotalAdultCount => _personLines.Sum(l => l.AdultCount);
    public int TotalChildrenUnder3Count => _personLines.Sum(l => l.ChildrenUnder3Count);

    private static int NormalizeBedCount(Enums.RentalMode rentalMode, int studioBeds, int bedCount)
    {
        if (rentalMode != Enums.RentalMode.PerBed)
            return 0;

        if (bedCount < 1)
            throw new ArgumentException("At least one bed is required for a per-bed reservation.");
        if (bedCount > studioBeds)
            throw new InvalidOperationException(
                $"Requested {bedCount} bed(s) but the studio only has {studioBeds}.");

        return bedCount;
    }

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
