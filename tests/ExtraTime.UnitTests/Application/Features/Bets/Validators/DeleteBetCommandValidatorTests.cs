using ExtraTime.Application.Features.Bets.Commands.DeleteBet;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Bets.Validators;

public sealed class DeleteBetCommandValidatorTests : ValidatorTestBase<DeleteBetCommandValidator, DeleteBetCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new DeleteBetCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyLeagueId_HasError()
    {
        var command = new DeleteBetCommand(Guid.Empty, Guid.NewGuid());
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
    }

    [Test]
    public async Task Validate_EmptyBetId_HasError()
    {
        var command = new DeleteBetCommand(Guid.NewGuid(), Guid.Empty);
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.BetId);
    }

    [Test]
    public async Task Validate_BothIdsEmpty_HasMultipleErrors()
    {
        var command = new DeleteBetCommand(Guid.Empty, Guid.Empty);
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
        result.ShouldHaveValidationErrorFor(x => x.BetId);
    }
}
