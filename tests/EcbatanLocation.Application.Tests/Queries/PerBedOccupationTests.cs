using EcbatanLocation.Application.Commands.AcceptReservation;
using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Application.Queries.GetDailyOccupation;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcbatanLocation.Application.Tests.Queries;

public class PerBedOccupationTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task DailyOccupation_PerBedStudio_CountsOccupiedBedsNotFullCapacity()
    {
        // "Emplacement tente 1": rentable alone, capacity 4 — switch it to per-bed with 4 beds.
        var studio = await GetStudioAsync("Emplacement tente 1");
        studio.Update(studio.Name, studio.Capacity, studio.HasKitchen, studio.RentableAlone,
            unavailable: false, RentalMode.PerBed, numberOfBeds: 4);
        var studioRepo = Services.GetRequiredService<IStudioRepository>();
        await studioRepo.UpdateAsync(studio);

        var owner = await GetOwnerAsync("Léa");
        var start = new DateOnly(2026, 7, 1);
        var end = new DateOnly(2026, 7, 8);

        // 3 beds occupied out of 4 (2 + 1), both accepted so they count toward occupation.
        var id1 = await Mediator.Send(new CreateReservationCommand(studio.Id, owner.Id, start, end,
            "A", [new PersonLineDto(ClientType.Tent, 2, 0)], null, BedCount: 2));
        var id2 = await Mediator.Send(new CreateReservationCommand(studio.Id, owner.Id, start, end,
            "B", [new PersonLineDto(ClientType.Tent, 1, 0)], null, BedCount: 1));

        await Mediator.Send(new AcceptReservationCommand(id1, "Léa"));
        await Mediator.Send(new AcceptReservationCommand(id2, "Léa"));

        var occ = await Mediator.Send(new GetDailyOccupationQuery(new DateOnly(2026, 7, 3)));

        // Occupied beds (3), not the full capacity (4).
        Assert.Equal(3, occ.OccupiedPlaces);
    }

    [Fact]
    public async Task DailyOccupation_PerBedStudio_PendingReservationsDoNotCount()
    {
        var studio = await GetStudioAsync("Emplacement tente 1");
        studio.Update(studio.Name, studio.Capacity, studio.HasKitchen, studio.RentableAlone,
            unavailable: false, RentalMode.PerBed, numberOfBeds: 4);
        var studioRepo = Services.GetRequiredService<IStudioRepository>();
        await studioRepo.UpdateAsync(studio);

        var owner = await GetOwnerAsync("Léa");
        var start = new DateOnly(2026, 7, 1);
        var end = new DateOnly(2026, 7, 8);

        // Left Pending: must not be counted as occupied (H2).
        await Mediator.Send(new CreateReservationCommand(studio.Id, owner.Id, start, end,
            "A", [new PersonLineDto(ClientType.Tent, 2, 0)], null, BedCount: 2));

        var occ = await Mediator.Send(new GetDailyOccupationQuery(new DateOnly(2026, 7, 3)));

        Assert.Equal(0, occ.OccupiedPlaces);
    }
}
