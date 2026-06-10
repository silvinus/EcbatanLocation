using FluentValidation;

namespace EcbatanLocation.Application.Commands.AcceptReservation;

public class AcceptReservationCommandValidator : AbstractValidator<AcceptReservationCommand>
{
    public AcceptReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
        RuleFor(x => x.AcceptedBy).NotEmpty().MaximumLength(200);
    }
}
