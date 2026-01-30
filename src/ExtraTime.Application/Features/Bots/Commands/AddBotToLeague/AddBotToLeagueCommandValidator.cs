using FluentValidation;

namespace ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;

public sealed class AddBotToLeagueCommandValidator : AbstractValidator<AddBotToLeagueCommand>
{
    public AddBotToLeagueCommandValidator()
    {
        RuleFor(x => x.LeagueId)
            .NotEmpty();

        RuleFor(x => x.BotId)
            .NotEmpty();
    }
}
