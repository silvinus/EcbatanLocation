using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.Events;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Domain.Tests.Entities;

public class ReservationDomainEventsTests
{
    private static Reservation Create()
        => Reservation.Create(Guid.NewGuid(), Guid.NewGuid(),
            new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8)),
            "Dupont", [new PersonLine(ClientType.Owner, 2, 0)], 6);

    [Fact]
    public void Create_RaisesReservationCreated()
    {
        var reservation = Create();

        var evt = Assert.Single(reservation.DomainEvents);
        var created = Assert.IsType<ReservationCreated>(evt);
        Assert.Equal(reservation.Id, created.ReservationId);
    }

    [Fact]
    public void Accept_RaisesReservationAccepted()
    {
        var reservation = Create();
        reservation.ClearDomainEvents();

        reservation.Accept("Léa");

        var accepted = Assert.IsType<ReservationAccepted>(Assert.Single(reservation.DomainEvents));
        Assert.Equal("Léa", accepted.AcceptedBy);
    }

    [Fact]
    public void Confirm_RaisesReservationConfirmed()
    {
        var reservation = Create();
        reservation.Accept("Léa");
        reservation.ClearDomainEvents();

        reservation.Confirm("Jean");

        var confirmed = Assert.IsType<ReservationConfirmed>(Assert.Single(reservation.DomainEvents));
        Assert.Equal("Jean", confirmed.ConfirmedBy);
    }

    [Fact]
    public void MarkDeleted_RaisesReservationDeleted()
    {
        var reservation = Create();
        reservation.ClearDomainEvents();

        reservation.MarkDeleted();

        var deleted = Assert.IsType<ReservationDeleted>(Assert.Single(reservation.DomainEvents));
        Assert.Equal(reservation.Id, deleted.ReservationId);
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheCollection()
    {
        var reservation = Create();

        reservation.ClearDomainEvents();

        Assert.Empty(reservation.DomainEvents);
    }
}
