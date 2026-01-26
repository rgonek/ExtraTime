using ExtraTime.Application.Features.Leagues.Commands.CreateLeague;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Validators;

public sealed class CreateLeagueCommandValidatorTests : ValidatorTestBase<CreateLeagueCommandValidator, CreateLeagueCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new CreateLeagueCommand(
            "Test League", 
            "Description", 
            false, 
            20, 
            3, 
            1, 
            5, 
            null, 
            null);
        
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyName_HasError()
    {
        var command = new CreateLeagueCommand(
            "", 
            null, 
            false, 
            20, 
            3, 
            1, 
            5, 
            null, 
            null);
            
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Validate_InvalidMaxMembers_HasError()
    {
        var command = new CreateLeagueCommand(
            "Test League", 
            null, 
            false, 
            300, 
            3, 
            1, 
            5, 
            null, 
            null);
            
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.MaxMembers);
    }

    [Test]
    public async Task Validate_InvalidScoring_HasError()
    {
        var command = new CreateLeagueCommand(
            "Test League", 
            null, 
            false, 
            20, 
            -1, 
            1, 
            5, 
            null, 
            null);
            
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ScoreExactMatch);
    }
}
