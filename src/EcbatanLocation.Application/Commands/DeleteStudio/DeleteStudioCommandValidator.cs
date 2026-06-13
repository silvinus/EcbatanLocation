using FluentValidation;

namespace EcbatanLocation.Application.Commands.DeleteStudio;

public class DeleteStudioCommandValidator : AbstractValidator<DeleteStudioCommand>
{
    public DeleteStudioCommandValidator()
    {
        RuleFor(x => x.StudioId).NotEmpty();
    }
}
