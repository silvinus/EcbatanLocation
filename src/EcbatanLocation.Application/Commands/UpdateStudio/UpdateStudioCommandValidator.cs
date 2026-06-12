using FluentValidation;

namespace EcbatanLocation.Application.Commands.UpdateStudio;

public class UpdateStudioCommandValidator : AbstractValidator<UpdateStudioCommand>
{
    public UpdateStudioCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(1);
    }
}
