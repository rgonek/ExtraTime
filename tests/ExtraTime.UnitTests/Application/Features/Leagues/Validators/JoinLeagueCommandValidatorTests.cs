using ExtraTime.Application.Features.Leagues.Commands.JoinLeague;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Validators;

public sealed class JoinLeagueCommandValidatorTests : ValidatorTestBase<JoinLeagueCommandValidator, JoinLeagueCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new JoinLeagueCommand(Guid.NewGuid(), "VALID123");
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyLeagueId_HasError()
    {
        var command = new JoinLeagueCommand(Guid.Empty, "VALID123");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
    }

    [Test]
    public async Task Validate_EmptyInviteCode_HasError()
    {
        var command = new JoinLeagueCommand(Guid.NewGuid(), "");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Test]
    public async Task Validate_ShortInviteCode_HasError()
    {
        var command = new JoinLeagueCommand(Guid.NewGuid(), "SHORT1");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }

    [Test]
    public async Task Validate_LongInviteCode_HasError()
    {
        var command = new JoinLeagueCommand(Guid.NewGuid(), "TOOLONG123");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.InviteCode);
    }
}
