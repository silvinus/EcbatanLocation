using System.Text.Json;
using System.Text.Json.Serialization;
using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Queries.GetMonthlyPlanning;

namespace EcbatanLocation.Web.Endpoints;

/// <summary>
/// Read-only JSON access to the monthly planning, for external consumers (e.g. an MCP tool).
/// Exposes exactly what the public planning page already shows (assumption H4), so it is anonymous.
/// </summary>
public static class PlanningApiEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static IEndpointRouteBuilder MapPlanningApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/planning", async (int year, int month, IMediator mediator, CancellationToken cancellationToken) =>
        {
            if (month is < 1 or > 12 || year is < 2000 or > 2100)
                return Results.BadRequest(new { error = "Expected year in 2000-2100 and month in 1-12." });

            var planning = await mediator.Send(new GetMonthlyPlanningQuery(year, month), cancellationToken);
            return Results.Json(planning, JsonOptions);
        })
        .AllowAnonymous()
        .RequireRateLimiting("fixed");

        return endpoints;
    }
}
