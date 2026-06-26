using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Tests.Services;

public class PerBedAvailabilityTests
{
    private readonly ReservationDomainService _service = new();

    private static readonly DateRange July = new(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 8));

    private static Studio PerBedStudio(int capacity = 4, int beds = 4)
        => Studio.Create("Studio Centre", capacity, false, false, 4, false, RentalMode.PerBed, beds);

    private static Reservation BedReservation(Studio studio, int beds, int adults)
        => Reservation.Create(
            studio.Id, Guid.NewGuid(), July, "Tenant",
            [new PersonLine(ClientType.Owner, adults, 0)],
            studio.Capacity, studio.RentalMode, studio.NumberOfBeds, beds);

    [Fact]
    public void ValidateBedAvailability_WithinBedsAndCapacity_Passes()
    {
        var studio = PerBedStudio(capacity: 4, beds: 4);
        var existing = new[] { BedReservation(studio, beds: 1, adults: 1) };

        _service.ValidateBedAvailability(studio, requestedBeds: 2, requestedAdults: 2, existing);
    }

    [Fact]
    public void ValidateBedAvailability_FillsExactlyToCapacity_Passes()
    {
        var studio = PerBedStudio(capacity: 4, beds: 4);
        var existing = new[]
        {
            BedReservation(studio, beds: 1, adults: 1),
            BedReservation(studio, beds: 2, adults: 2)
        };

        _service.ValidateBedAvailability(studio, requestedBeds: 1, requestedAdults: 1, existing);
    }

    [Fact]
    public void ValidateBedAvailability_ExceedsRemainingBeds_Throws()
    {
        var studio = PerBedStudio(capacity: 8, beds: 4);
        var existing = new[] { BedReservation(studio, beds: 3, adults: 1) };

        Assert.Throws<NoBedsAvailableException>(() =>
            _service.ValidateBedAvailability(studio, requestedBeds: 2, requestedAdults: 1, existing));
    }

    [Fact]
    public void ValidateBedAvailability_ExceedsCapacity_Throws()
    {
        // Beds fit (1 + 1 <= 4) but people don't (3 + 2 > 4).
        var studio = PerBedStudio(capacity: 4, beds: 4);
        var existing = new[] { BedReservation(studio, beds: 1, adults: 3) };

        Assert.Throws<NoBedsAvailableException>(() =>
            _service.ValidateBedAvailability(studio, requestedBeds: 1, requestedAdults: 2, existing));
    }

    [Fact]
    public void ValidateBedAvailability_RequestMoreThanTotalBeds_Throws()
    {
        var studio = PerBedStudio(capacity: 8, beds: 4);

        Assert.Throws<NoBedsAvailableException>(() =>
            _service.ValidateBedAvailability(studio, requestedBeds: 5, requestedAdults: 1, []));
    }

    [Fact]
    public void ValidateBedAvailability_NonPerBedStudio_Throws()
    {
        var lodging = Studio.Create("Villa", 6, true, true, 1);

        Assert.Throws<InvalidOperationException>(() =>
            _service.ValidateBedAvailability(lodging, requestedBeds: 1, requestedAdults: 1, []));
    }

    [Fact]
    public void Reservation_Create_PerBed_BedCountAboveStudioBeds_Throws()
    {
        var studio = PerBedStudio(capacity: 8, beds: 4);

        Assert.Throws<InvalidOperationException>(() => BedReservation(studio, beds: 5, adults: 1));
    }

    [Fact]
    public void Reservation_Create_PerBed_ZeroBeds_Throws()
    {
        var studio = PerBedStudio(capacity: 4, beds: 4);

        Assert.Throws<ArgumentException>(() => BedReservation(studio, beds: 0, adults: 1));
    }

    [Fact]
    public void Reservation_Create_PerLodging_StoresZeroBeds()
    {
        var studioId = Guid.NewGuid();
        var res = Reservation.Create(
            studioId, Guid.NewGuid(), July, "Tenant",
            [new PersonLine(ClientType.Owner, 2, 0)], 6);

        Assert.Equal(0, res.BedCount);
    }

    [Fact]
    public void Studio_Create_PerBed_BedsAboveCapacity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Studio.Create("Studio Centre", 2, false, false, 4, false, RentalMode.PerBed, 3));
    }

    [Fact]
    public void Studio_Create_PerLodging_ForcesZeroBeds()
    {
        var studio = Studio.Create("Villa", 6, true, true, 1, false, RentalMode.PerLodging, 4);

        Assert.Equal(0, studio.NumberOfBeds);
        Assert.False(studio.IsPerBed);
    }

    private static Studio LodgingStudio() => Studio.Create("Villa", 6, true, true, 1);

    private static Reservation LodgingReservation(Studio studio, int adults = 2)
        => Reservation.Create(studio.Id, Guid.NewGuid(), July, "Tenant",
            [new PersonLine(ClientType.Owner, adults, 0)], studio.Capacity);

    [Fact]
    public void CanAccommodate_PerLodging_NoRealOverlap_True()
    {
        var studio = LodgingStudio();
        var candidate = LodgingReservation(studio);

        Assert.True(_service.CanAccommodate(studio, candidate, []));
    }

    [Fact]
    public void CanAccommodate_PerLodging_RealOverlapExists_False()
    {
        var studio = LodgingStudio();
        var candidate = LodgingReservation(studio);
        var existing = new[] { LodgingReservation(studio) };

        Assert.False(_service.CanAccommodate(studio, candidate, existing));
    }

    [Fact]
    public void CanAccommodate_PerBed_FitsWithinBedsAndCapacity_True()
    {
        var studio = PerBedStudio(capacity: 4, beds: 4);
        var candidate = BedReservation(studio, beds: 2, adults: 2);
        var existing = new[] { BedReservation(studio, beds: 1, adults: 1) };

        Assert.True(_service.CanAccommodate(studio, candidate, existing));
    }

    [Fact]
    public void CanAccommodate_PerBed_ExceedsBeds_False()
    {
        var studio = PerBedStudio(capacity: 8, beds: 4);
        var candidate = BedReservation(studio, beds: 2, adults: 1);
        var existing = new[] { BedReservation(studio, beds: 3, adults: 1) };

        Assert.False(_service.CanAccommodate(studio, candidate, existing));
    }
}
