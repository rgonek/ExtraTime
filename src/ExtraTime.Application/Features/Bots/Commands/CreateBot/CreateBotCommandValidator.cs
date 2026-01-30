using FluentValidation;

namespace ExtraTime.Application.Features.Bots.Commands.CreateBot;

public sealed class CreateBotCommandValidator : AbstractValidator<CreateBotCommand>
{
    public CreateBotCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
    }
}
