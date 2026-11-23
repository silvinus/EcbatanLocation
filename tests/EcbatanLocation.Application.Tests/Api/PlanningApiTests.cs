using System.Net;
using System.Text.Json;
using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.Tests.Api;

public class PlanningApiTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetPlanning_Anonymous_ReturnsMonthReservationsWithStatusAsText()
    {
        var villa = await GetStudioAsync("Villa");
        var owner = await GetOwnerAsync("Léa");
        await Mediator.Send(new CreateReservationCommand(villa.Id, owner.Id,
            new DateOnly(2026, 7, 3), new DateOnly(2026, 7, 10),
            "Dupont", [new PersonLineDto(ClientType.Owner, 2, 1)]));

        using var client = Fixture.CreateClient();
        var response = await client.GetAsync("/api/planning?year=2026&month=7");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(2026, root.GetProperty("year").GetInt32());
        Assert.Equal(7, root.GetProperty("month").GetInt32());

        var villaPlanning = root.GetProperty("studios").EnumerateArray()
            .Single(s => s.GetProperty("studio").GetProperty("name").GetString() == "Villa");
        var reservation = Assert.Single(villaPlanning.GetProperty("reservations").EnumerateArray());
        Assert.Equal("Dupont", reservation.GetProperty("tenantName").GetString());
        Assert.Equal("Léa", reservation.GetProperty("ownerName").GetString());
        Assert.Equal("2026-07-03", reservation.GetProperty("startDate").GetString());
        Assert.Equal("Pending", reservation.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("year=2026&month=13")]
    [InlineData("year=2026&month=0")]
    [InlineData("year=1999&month=5")]
    public async Task GetPlanning_InvalidPeriod_ReturnsBadRequest(string query)
    {
        using var client = Fixture.CreateClient();
        var response = await client.GetAsync($"/api/planning?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
