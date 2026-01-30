using ExtraTime.Application.Features.Bots.Commands.AddBotToLeague;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Bots.Validators;

public sealed class AddBotToLeagueCommandValidatorTests : ValidatorTestBase<AddBotToLeagueCommandValidator, AddBotToLeagueCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new AddBotToLeagueCommand(
            Guid.NewGuid(),
            Guid.NewGuid());

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyLeagueId_HasError()
    {
        var command = new AddBotToLeagueCommand(
            Guid.Empty,
            Guid.NewGuid());

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
    }

    [Test]
    public async Task Validate_EmptyBotId_HasError()
    {
        var command = new AddBotToLeagueCommand(
            Guid.NewGuid(),
            Guid.Empty);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.BotId);
    }

    [Test]
    public async Task Validate_BothIdsEmpty_HasMultipleErrors()
    {
        var command = new AddBotToLeagueCommand(
            Guid.Empty,
            Guid.Empty);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
        result.ShouldHaveValidationErrorFor(x => x.BotId);
    }

    [Test]
    public async Task Validate_ValidGuids_HasNoErrors()
    {
        var command = new AddBotToLeagueCommand(
            new Guid("12345678-1234-1234-1234-123456789012"),
            new Guid("87654321-4321-4321-4321-210987654321"));

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }
}
