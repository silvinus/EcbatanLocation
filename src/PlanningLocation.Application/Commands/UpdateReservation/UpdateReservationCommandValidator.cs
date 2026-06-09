using FluentValidation;

namespace PlanningLocation.Application.Commands.UpdateReservation;

public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
{
    public UpdateReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.");
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AdultCount).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ChildrenUnder3Count).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ClientType).IsInEnum();
    }
}
