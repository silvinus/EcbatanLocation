using FluentValidation;

namespace EcbatanLocation.Application.Commands.CreateStudio;

public class CreateStudioCommandValidator : AbstractValidator<CreateStudioCommand>
{
    public CreateStudioCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(1);
    }
}
