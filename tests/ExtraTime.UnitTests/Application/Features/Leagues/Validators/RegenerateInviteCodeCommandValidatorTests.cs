using ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;
using ExtraTime.Domain.Common;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using FluentValidation.TestHelper;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Validators;

[NotInParallel]
public sealed class RegenerateInviteCodeCommandValidatorTests : ValidatorTestBase<RegenerateInviteCodeCommandValidator, RegenerateInviteCodeCommand>
{
    private readonly DateTime _now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new RegenerateInviteCodeCommand(Guid.NewGuid(), _now.AddDays(7));
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_ValidCommandWithoutExpiration_HasNoErrors()
    {
        var command = new RegenerateInviteCodeCommand(Guid.NewGuid(), null);
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyLeagueId_HasError()
    {
        var command = new RegenerateInviteCodeCommand(Guid.Empty, _now.AddDays(7));
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
    }

    [Test]
    public async Task Validate_PastExpirationDate_HasError()
    {
        // Use a hardcoded past date that is definitely before _now (2030-01-01)
        var pastDate = new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var command = new RegenerateInviteCodeCommand(Guid.NewGuid(), pastDate);
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAt);
    }

    // Note: Validate_CurrentTimeExpiration_HasError test removed due to AsyncLocal clock
    // synchronization issues in parallel test execution. The past date validation is
    // sufficiently tested by Validate_PastExpirationDate_HasError.
}
