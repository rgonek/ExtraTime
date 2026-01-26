using ExtraTime.Application.Features.Bets.Commands.PlaceBet;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Bets.Validators;

public sealed class PlaceBetCommandValidatorTests : ValidatorTestBase<PlaceBetCommandValidator, PlaceBetCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new PlaceBetCommand(Guid.NewGuid(), Guid.NewGuid(), 2, 1);
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_NegativeScore_HasError()
    {
        var command = new PlaceBetCommand(Guid.NewGuid(), Guid.NewGuid(), -1, 1);
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.PredictedHomeScore);
    }

    [Test]
    public async Task Validate_EmptyIds_HasError()
    {
        var command = new PlaceBetCommand(Guid.Empty, Guid.Empty, 2, 1);
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
        result.ShouldHaveValidationErrorFor(x => x.MatchId);
    }
}
