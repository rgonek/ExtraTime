using FluentValidation;

namespace ExtraTime.Application.Features.Leagues.Commands.JoinLeague;

public sealed class JoinLeagueCommandValidator : AbstractValidator<JoinLeagueCommand>
{
    public JoinLeagueCommandValidator()
    {
        RuleFor(x => x.LeagueId)
            .NotEmpty().WithMessage("League ID is required.");

        RuleFor(x => x.InviteCode)
            .NotEmpty().WithMessage("Invite code is required.")
            .Length(8).WithMessage("Invite code must be 8 characters.");
    }
}
