using FluentValidation;

namespace ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;

public sealed class RegenerateInviteCodeCommandValidator : AbstractValidator<RegenerateInviteCodeCommand>
{
    public RegenerateInviteCodeCommandValidator()
    {
        RuleFor(x => x.LeagueId)
            .NotEmpty().WithMessage("League ID is required.");

        RuleFor(x => x.ExpiresAt)
            .Must(date => date > DateTime.UtcNow).WithMessage("Invite code expiration must be in the future.")
            .When(x => x.ExpiresAt.HasValue);
    }
}
