using ExtraTime.Application.Features.Bots.Commands.CreateBot;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Bots.Validators;

public sealed class CreateBotCommandValidatorTests : ValidatorTestBase<CreateBotCommandValidator, CreateBotCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new CreateBotCommand(
            "TestBot",
            "https://example.com/avatar.png",
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyName_HasError()
    {
        var command = new CreateBotCommand(
            "",
            null,
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Validate_NullName_HasError()
    {
        var command = new CreateBotCommand(
            null!,
            null,
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Validate_NameTooLong_HasError()
    {
        var command = new CreateBotCommand(
            new string('A', 51),
            null,
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public async Task Validate_NameAtMaxLength_HasNoErrors()
    {
        var command = new CreateBotCommand(
            new string('A', 50),
            null,
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyAvatarUrl_HasNoErrors()
    {
        var command = new CreateBotCommand(
            "TestBot",
            "",
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_NullAvatarUrl_HasNoErrors()
    {
        var command = new CreateBotCommand(
            "TestBot",
            null,
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_AvatarUrlTooLong_HasError()
    {
        var command = new CreateBotCommand(
            "TestBot",
            new string('A', 501),
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Test]
    public async Task Validate_AvatarUrlAtMaxLength_HasNoErrors()
    {
        var command = new CreateBotCommand(
            "TestBot",
            new string('A', 500),
            BotStrategy.Random,
            null);

        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_DifferentStrategies_HasNoErrors()
    {
        var strategies = new[]
        {
            BotStrategy.Random,
            BotStrategy.HomeFavorer,
            BotStrategy.UnderdogSupporter,
            BotStrategy.DrawPredictor,
            BotStrategy.HighScorer,
            BotStrategy.StatsAnalyst
        };

        foreach (var strategy in strategies)
        {
            var command = new CreateBotCommand(
                $"Bot_{strategy}",
                null,
                strategy,
                null);

            var result = await Validator.TestValidateAsync(command);
            await Assert.That(result.IsValid).IsTrue();
        }
    }
}
