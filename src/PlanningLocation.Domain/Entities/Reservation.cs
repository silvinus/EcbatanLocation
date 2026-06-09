using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid StudioId { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateRange Dates { get; private set; } = default!;
    public string TenantName { get; private set; } = default!;
    public int AdultCount { get; private set; }
    public int ChildrenUnder3Count { get; private set; }
    public ClientType ClientType { get; private set; }
    public ReservationStatus Status { get; private set; }
    public string? AcceptedBy { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public string? ConfirmedBy { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Reservation() { }

    public static Reservation Create(
        Guid studioId,
        Guid ownerId,
        DateRange dates,
        string tenantName,
        int adultCount,
        int childrenUnder3Count,
        ClientType clientType,
        int studioCapacity)
    {
        if (adultCount < 1)
            throw new ArgumentException("At least one adult is required.");
        if (childrenUnder3Count < 0)
            throw new ArgumentException("Children count cannot be negative.");
        if (string.IsNullOrWhiteSpace(tenantName))
            throw new ArgumentException("Tenant name is required.");
        if (adultCount + childrenUnder3Count > studioCapacity)
            throw new InvalidOperationException(
                $"Capacity exceeded: {adultCount + childrenUnder3Count} persons for a capacity of {studioCapacity}.");

        return new Reservation
        {
            Id = Guid.NewGuid(),
            StudioId = studioId,
            OwnerId = ownerId,
            Dates = dates,
            TenantName = tenantName,
            AdultCount = adultCount,
            ChildrenUnder3Count = childrenUnder3Count,
            ClientType = clientType,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Accept(string by)
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only a 'Pending' reservation can be accepted.");

        Status = ReservationStatus.Accepted;
        AcceptedBy = by;
        AcceptedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm(string by)
    {
        if (Status != ReservationStatus.Accepted)
            throw new InvalidOperationException("Only an 'Accepted' reservation can be confirmed.");

        Status = ReservationStatus.Confirmed;
        ConfirmedBy = by;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(
        DateRange dates,
        string tenantName,
        int adultCount,
        int childrenUnder3Count,
        ClientType clientType,
        int studioCapacity)
    {
        if (adultCount < 1)
            throw new ArgumentException("At least one adult is required.");
        if (childrenUnder3Count < 0)
            throw new ArgumentException("Children count cannot be negative.");
        if (adultCount + childrenUnder3Count > studioCapacity)
            throw new InvalidOperationException(
                $"Capacity exceeded: {adultCount + childrenUnder3Count} persons for a capacity of {studioCapacity}.");

        Dates = dates;
        TenantName = tenantName;
        AdultCount = adultCount;
        ChildrenUnder3Count = childrenUnder3Count;
        ClientType = clientType;
        UpdatedAt = DateTime.UtcNow;
    }

    public int TotalPersonCount => AdultCount + ChildrenUnder3Count;
}
