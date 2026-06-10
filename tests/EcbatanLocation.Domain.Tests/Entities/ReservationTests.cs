using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Tests.Entities;

public class ReservationTests
{
    private static readonly Guid StudioId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateRange Dates = new(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

    private static Reservation CreateReservation(
        int adultCount = 2,
        int childrenCount = 0,
        int capacity = 6,
        ClientType clientType = ClientType.Acquaintance)
    {
        var lines = new[] { new PersonLine(clientType, adultCount, childrenCount) };
        return Reservation.Create(
            StudioId, OwnerId, Dates,
            "Dupont", lines, capacity);
    }

    [Fact]
    public void Create_Valid_StatusIsPending()
    {
        var reservation = CreateReservation();

        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal("Dupont", reservation.TenantName);
        Assert.Equal(2, reservation.TotalAdultCount);
    }

    [Fact]
    public void Create_CapacityExceeded_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CreateReservation(adultCount: 4, childrenCount: 3, capacity: 6));
    }

    [Fact]
    public void Create_ExactCapacity_Succeeds()
    {
        var reservation = CreateReservation(adultCount: 4, childrenCount: 2, capacity: 6);
        Assert.Equal(6, reservation.TotalPersonCount);
    }

    [Fact]
    public void Create_ZeroAdults_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateReservation(adultCount: 0));
    }

    [Fact]
    public void Create_NegativeChildren_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateReservation(childrenCount: -1));
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var lines = new[] { new PersonLine(ClientType.Acquaintance, 2, 0) };
        Assert.Throws<ArgumentException>(() =>
            Reservation.Create(StudioId, OwnerId, Dates, "", lines, 6));
    }

    [Fact]
    public void Create_MultiplePersonLines_SumsCorrectly()
    {
        var lines = new[]
        {
            new PersonLine(ClientType.Owner, 2, 0),
            new PersonLine(ClientType.GuestWithPresence, 1, 1),
            new PersonLine(ClientType.Acquaintance, 0, 2)
        };
        var reservation = Reservation.Create(StudioId, OwnerId, Dates, "Dupont", lines, 6);

        Assert.Equal(3, reservation.PersonLines.Count);
        Assert.Equal(3, reservation.TotalAdultCount);
        Assert.Equal(3, reservation.TotalChildrenUnder3Count);
        Assert.Equal(6, reservation.TotalPersonCount);
    }

    [Fact]
    public void Create_MultipleLines_CapacityExceeded_Throws()
    {
        var lines = new[]
        {
            new PersonLine(ClientType.Owner, 3, 0),
            new PersonLine(ClientType.GuestWithPresence, 2, 2)
        };
        Assert.Throws<InvalidOperationException>(() =>
            Reservation.Create(StudioId, OwnerId, Dates, "Dupont", lines, 6));
    }

    [Fact]
    public void Create_EmptyLines_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reservation.Create(StudioId, OwnerId, Dates, "Dupont", Array.Empty<PersonLine>(), 6));
    }

    [Fact]
    public void Accept_FromPending_BecomesAccepted()
    {
        var reservation = CreateReservation();

        reservation.Accept("Jean");

        Assert.Equal(ReservationStatus.Accepted, reservation.Status);
        Assert.Equal("Jean", reservation.AcceptedBy);
        Assert.NotNull(reservation.AcceptedAt);
    }

    [Fact]
    public void Accept_FromAccepted_Throws()
    {
        var reservation = CreateReservation();
        reservation.Accept("Jean");

        Assert.Throws<InvalidOperationException>(() =>
            reservation.Accept("Léa"));
    }

    [Fact]
    public void Confirm_FromAccepted_BecomesConfirmed()
    {
        var reservation = CreateReservation();
        reservation.Accept("Jean");

        reservation.Confirm("Léa");

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.Equal("Léa", reservation.ConfirmedBy);
        Assert.NotNull(reservation.ConfirmedAt);
    }

    [Fact]
    public void Confirm_FromPending_Throws()
    {
        var reservation = CreateReservation();

        Assert.Throws<InvalidOperationException>(() =>
            reservation.Confirm("Jean"));
    }

    [Fact]
    public void Update_CapacityExceeded_Throws()
    {
        var reservation = CreateReservation(adultCount: 2, capacity: 6);
        var newDates = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5));
        var lines = new[] { new PersonLine(ClientType.Acquaintance, 5, 3) };

        Assert.Throws<InvalidOperationException>(() =>
            reservation.Update(newDates, "Martin", lines, 6));
    }

    [Fact]
    public void Update_Valid_UpdatesFields()
    {
        var reservation = CreateReservation();
        var newDates = new DateRange(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 5));
        var lines = new[]
        {
            new PersonLine(ClientType.GuestWithPresence, 3, 1)
        };

        reservation.Update(newDates, "Martin", lines, 6);

        Assert.Equal("Martin", reservation.TenantName);
        Assert.Equal(3, reservation.TotalAdultCount);
        Assert.Equal(1, reservation.TotalChildrenUnder3Count);
        Assert.Single(reservation.PersonLines);
        Assert.NotNull(reservation.UpdatedAt);
    }

    [Fact]
    public void TotalPersonCount_IsCorrect()
    {
        var reservation = CreateReservation(adultCount: 3, childrenCount: 2, capacity: 6);
        Assert.Equal(5, reservation.TotalPersonCount);
    }
}
