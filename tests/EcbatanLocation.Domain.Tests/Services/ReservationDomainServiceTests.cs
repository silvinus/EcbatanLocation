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

    // --- ValidateStudioDependency (H1) ---

    [Fact]
    public void ValidateStudioDependency_RentableAlone_Passes()
    {
        var studio = CreateStudio(rentableAlone: true);
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

        _service.ValidateStudioDependency(studio, Guid.NewGuid(), dates, []);
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_NoIndependentReservation_Throws()
    {
        var studio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateStudioDependency(studio, ownerId, dates, []));
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_ContainedByIndependentReservation_Passes()
    {
        var independentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 8));
        var independentDates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));

        var existingReservation = CreateReservation(independentStudio.Id, ownerId, independentDates);

        _service.ValidateStudioDependency(dependentStudio, ownerId, dates, [existingReservation]);
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_IdenticalDates_Passes()
    {
        var independentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

        var existingReservation = CreateReservation(independentStudio.Id, ownerId, dates);

        _service.ValidateStudioDependency(dependentStudio, ownerId, dates, [existingReservation]);
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_OverflowsAtStart_Throws()
    {
        var independentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));
        var independentDates = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10));

        var existingReservation = CreateReservation(independentStudio.Id, ownerId, independentDates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateStudioDependency(dependentStudio, ownerId, dates, [existingReservation]));
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_OverflowsAtEnd_Throws()
    {
        var independentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 15));
        var independentDates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 10));

        var existingReservation = CreateReservation(independentStudio.Id, ownerId, independentDates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateStudioDependency(dependentStudio, ownerId, dates, [existingReservation]));
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_OverflowsBothSides_Throws()
    {
        var independentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 15));
        var independentDates = new DateRange(new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10));

        var existingReservation = CreateReservation(independentStudio.Id, ownerId, independentDates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateStudioDependency(dependentStudio, ownerId, dates, [existingReservation]));
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_NonOverlappingReservation_Throws()
    {
        var independentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerId = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));
        var independentDates = new DateRange(new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 15));

        var existingReservation = CreateReservation(independentStudio.Id, ownerId, independentDates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateStudioDependency(dependentStudio, ownerId, dates, [existingReservation]));
    }

    [Fact]
    public void ValidateStudioDependency_NotRentableAlone_DifferentOwner_Throws()
    {
        var independentStudio = CreateStudio(rentableAlone: true, name: "Villa");
        var dependentStudio = CreateStudio(rentableAlone: false, name: "Studio Centre");
        var ownerA = Guid.NewGuid();
        var ownerB = Guid.NewGuid();
        var dates = new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

        var otherReservation = CreateReservation(independentStudio.Id, ownerB, dates);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateStudioDependency(dependentStudio, ownerA, dates, [otherReservation]));
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
