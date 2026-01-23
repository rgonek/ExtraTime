using FluentValidation;

namespace ExtraTime.Application.Features.Leagues.Commands.CreateLeague;

public sealed class CreateLeagueCommandValidator : AbstractValidator<CreateLeagueCommand>
{
    public CreateLeagueCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("League name is required.")
            .MinimumLength(3).WithMessage("League name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("League name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.MaxMembers)
            .InclusiveBetween(2, 255).WithMessage("Max members must be between 2 and 255.");

        RuleFor(x => x.ScoreExactMatch)
            .InclusiveBetween(0, 100).WithMessage("Score for exact match must be between 0 and 100.");

        RuleFor(x => x.ScoreCorrectResult)
            .InclusiveBetween(0, 100).WithMessage("Score for correct result must be between 0 and 100.");

        RuleFor(x => x.BettingDeadlineMinutes)
            .InclusiveBetween(0, 120).WithMessage("Betting deadline must be between 0 and 120 minutes.");

        RuleFor(x => x.InviteCodeExpiresAt)
            .Must(date => date > DateTime.UtcNow).WithMessage("Invite code expiration must be in the future.")
            .When(x => x.InviteCodeExpiresAt.HasValue);
    }
}
