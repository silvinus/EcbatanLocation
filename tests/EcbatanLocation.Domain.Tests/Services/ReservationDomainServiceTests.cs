using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Tests.Services;

public class ReservationDomainServiceTests
{
    private readonly ReservationDomainService _service = new();

    private static Studio CreateStudio(bool rentableAlone = true, string name = "Villa")
    {
        return Studio.Create(name, 6, true, rentableAlone, 1);
    }

    private static Reservation CreateReservation(Guid studioId, Guid ownerId, DateRange dates)
    {
        var lines = new[] { new PersonLine(ClientType.Acquaintance, 2, 0) };
        return Reservation.Create(
            studioId, ownerId, dates,
            "Dupont", lines, 6);
    }

    // --- ValidateParentLink ---

    [Fact]
    public void ValidateParentLink_ValidLink_Passes()
    {
        var parentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var parentDates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));
        var dependentDates = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(parentStudio.Id, ownerId, parentDates);

        _service.ValidateParentLink(dependentStudio, parent, parentStudio, dependentDates, ownerId);
    }

    [Fact]
    public void ValidateParentLink_IdenticalDates_Passes()
    {
        var parentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(parentStudio.Id, ownerId, dates);

        _service.ValidateParentLink(dependentStudio, parent, parentStudio, dates, ownerId);
    }

    [Fact]
    public void ValidateParentLink_DependentIsRentableAlone_Throws()
    {
        var parentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: true, name: "Studio Est");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(parentStudio.Id, ownerId, dates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateParentLink(dependentStudio, parent, parentStudio, dates, ownerId));
    }

    [Fact]
    public void ValidateParentLink_ParentNotRentableAlone_Throws()
    {
        var parentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Mobil-home");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(parentStudio.Id, ownerId, dates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateParentLink(dependentStudio, parent, parentStudio, dates, ownerId));
    }

    [Fact]
    public void ValidateParentLink_DifferentOwner_Throws()
    {
        var parentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(parentStudio.Id, ownerA, dates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateParentLink(dependentStudio, parent, parentStudio, dates, ownerB));
    }

    [Fact]
    public void ValidateParentLink_DatesNotContained_OverflowStart_Throws()
    {
        var parentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var parentDates = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10));
        var dependentDates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(parentStudio.Id, ownerId, parentDates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateParentLink(dependentStudio, parent, parentStudio, dependentDates, ownerId));
    }

    [Fact]
    public void ValidateParentLink_DatesNotContained_OverflowEnd_Throws()
    {
        var parentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var parentDates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));
        var dependentDates = new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 15));
        var parent = CreateReservation(parentStudio.Id, ownerId, parentDates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateParentLink(dependentStudio, parent, parentStudio, dependentDates, ownerId));
    }

    [Fact]
    public void ValidateParentLink_ParentIsChained_Throws()
    {
        var parentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(parentStudio.Id, ownerId, dates);
        parent.SetParentReservation(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateParentLink(dependentStudio, parent, parentStudio, dates, ownerId));
    }

    // --- PropagateStatusToDependents ---

    [Fact]
    public void PropagateStatusToDependents_CopiesStatusToAll()
    {
        var studioId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(studioId, ownerId, dates);
        parent.Accept("Jean");

        var dep1 = CreateReservation(Guid.NewGuid(), ownerId, dates);
        var dep2 = CreateReservation(Guid.NewGuid(), ownerId, dates);

        _service.PropagateStatusToDependents(parent, [dep1, dep2]);

        Assert.Equal(ReservationStatus.Accepted, dep1.Status);
        Assert.Equal("Jean", dep1.AcceptedBy);
        Assert.Equal(ReservationStatus.Accepted, dep2.Status);
        Assert.Equal("Jean", dep2.AcceptedBy);
    }

    [Fact]
    public void PropagateStatusToDependents_Confirmed_CopiesAllFields()
    {
        var studioId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var parent = CreateReservation(studioId, ownerId, dates);
        parent.Accept("Jean");
        parent.Confirm("Léa");

        var dep = CreateReservation(Guid.NewGuid(), ownerId, dates);

        _service.PropagateStatusToDependents(parent, [dep]);

        Assert.Equal(ReservationStatus.Confirmed, dep.Status);
        Assert.Equal("Jean", dep.AcceptedBy);
        Assert.Equal("Léa", dep.ConfirmedBy);
        Assert.NotNull(dep.AcceptedAt);
        Assert.NotNull(dep.ConfirmedAt);
    }

    // --- ValidateNoOverlap ---

    [Fact]
    public void ValidateNoOverlap_NoOverlap_Passes()
    {
        _service.ValidateNoOverlap(false);
    }

    [Fact]
    public void ValidateNoOverlap_OverlapExists_Throws()
    {
        Assert.Throws<OverlappingReservationException>(() =>
            _service.ValidateNoOverlap(true));
    }
}
