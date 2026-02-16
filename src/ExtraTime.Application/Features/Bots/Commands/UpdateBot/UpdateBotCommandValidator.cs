using FluentValidation;

namespace ExtraTime.Application.Features.Bots.Commands.UpdateBot;

public sealed class UpdateBotCommandValidator : AbstractValidator<UpdateBotCommand>
{
    public UpdateBotCommandValidator()
    {
        RuleFor(x => x.BotId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MaximumLength(50)
            .When(x => x.Name is not null);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .When(x => x.AvatarUrl is not null);
    }
}
