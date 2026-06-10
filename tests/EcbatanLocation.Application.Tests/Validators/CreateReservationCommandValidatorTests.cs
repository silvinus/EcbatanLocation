using EcbatanLocation.Application.Commands.CreateReservation;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Application.Tests.Validators;

public class CreateReservationCommandValidatorTests
{
    private readonly CreateReservationCommandValidator _validator = new();

    private static CreateReservationCommand Valid(
        string tenant = "Dupont",
        DateOnly? start = null,
        DateOnly? end = null,
        PersonLineDto[]? lines = null)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            start ?? new DateOnly(2026, 7, 1),
            end ?? new DateOnly(2026, 7, 8),
            tenant,
            lines ?? [new PersonLineDto(ClientType.Owner, 2, 0)]);

    [Fact]
    public void Valid_Command_Passes()
        => Assert.True(_validator.Validate(Valid()).IsValid);

    [Fact]
    public void EmptyTenant_Fails()
        => Assert.False(_validator.Validate(Valid(tenant: "")).IsValid);

    [Fact]
    public void EndBeforeStart_Fails()
        => Assert.False(_validator.Validate(
            Valid(start: new DateOnly(2026, 7, 8), end: new DateOnly(2026, 7, 1))).IsValid);

    [Fact]
    public void NoPersonLines_Fails()
        => Assert.False(_validator.Validate(Valid(lines: [])).IsValid);

    [Fact]
    public void NoAdults_Fails()
        => Assert.False(_validator.Validate(
            Valid(lines: [new PersonLineDto(ClientType.Owner, 0, 2)])).IsValid);

    [Fact]
    public void EmptyStudioId_Fails()
    {
        var cmd = Valid() with { StudioId = Guid.Empty };
        Assert.False(_validator.Validate(cmd).IsValid);
    }
}
