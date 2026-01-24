using FluentValidation;

namespace ExtraTime.Application.Features.Bets.Commands.PlaceBet;

public sealed class PlaceBetCommandValidator : AbstractValidator<PlaceBetCommand>
{
    public PlaceBetCommandValidator()
    {
        RuleFor(x => x.LeagueId)
            .NotEmpty().WithMessage("League ID is required.");

        RuleFor(x => x.MatchId)
            .NotEmpty().WithMessage("Match ID is required.");

        RuleFor(x => x.PredictedHomeScore)
            .InclusiveBetween(0, 99).WithMessage(BetErrors.InvalidScore);

        RuleFor(x => x.PredictedAwayScore)
            .InclusiveBetween(0, 99).WithMessage(BetErrors.InvalidScore);
    }
}
