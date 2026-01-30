using ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Validators;

public sealed class UpdateLeagueCommandValidatorTests : ValidatorTestBase<UpdateLeagueCommandValidator, UpdateLeagueCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: "Description",
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyLeagueId_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.Empty,
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.LeagueId);
    }

    [Test]
    public async Task Validate_EmptyName_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Validate_ShortName_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "AB",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Validate_LongName_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: new string('A', 101),
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Validate_LongDescription_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: new string('A', 501),
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public async Task Validate_LowMaxMembers_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 1,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.MaxMembers);
    }

    [Test]
    public async Task Validate_HighMaxMembers_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 256,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.MaxMembers);
    }

    [Test]
    public async Task Validate_NegativeScoreExactMatch_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: -1,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ScoreExactMatch);
    }

    [Test]
    public async Task Validate_HighScoreExactMatch_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 101,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ScoreExactMatch);
    }

    [Test]
    public async Task Validate_NegativeScoreCorrectResult_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: -1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ScoreCorrectResult);
    }

    [Test]
    public async Task Validate_HighScoreCorrectResult_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 101,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ScoreCorrectResult);
    }

    [Test]
    public async Task Validate_NegativeBettingDeadline_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: -1,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.BettingDeadlineMinutes);
    }

    [Test]
    public async Task Validate_HighBettingDeadline_HasError()
    {
        var command = new UpdateLeagueCommand(
            LeagueId: Guid.NewGuid(),
            Name: "Test League",
            Description: null,
            IsPublic: false,
            MaxMembers: 20,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 121,
            AllowedCompetitionIds: null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.BettingDeadlineMinutes);
    }
}
