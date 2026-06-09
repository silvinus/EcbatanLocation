using FluentValidation;

namespace PlanningLocation.Application.Commands.AcceptReservation;

public class AcceptReservationCommandValidator : AbstractValidator<AcceptReservationCommand>
{
    public AcceptReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.AcceptedBy).NotEmpty().MaximumLength(200);
    }
}
