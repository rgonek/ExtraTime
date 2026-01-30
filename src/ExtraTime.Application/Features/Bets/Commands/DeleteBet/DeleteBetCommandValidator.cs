using FluentValidation;

namespace ExtraTime.Application.Features.Bets.Commands.DeleteBet;

public sealed class DeleteBetCommandValidator : AbstractValidator<DeleteBetCommand>
{
    public DeleteBetCommandValidator()
    {
        RuleFor(x => x.LeagueId)
            .NotEmpty().WithMessage("League ID is required.");

        RuleFor(x => x.BetId)
            .NotEmpty().WithMessage("Bet ID is required.");
    }
}
