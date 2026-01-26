using ExtraTime.Application.Features.Auth.Commands.Register;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Auth.Validators;

public sealed class RegisterCommandValidatorTests : ValidatorTestBase<RegisterCommandValidator, RegisterCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new RegisterCommand("test@example.com", "testuser", "Password123!");
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_ShortUsername_HasError()
    {
        var command = new RegisterCommand("test@example.com", "us", "Password123!");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Test]
    public async Task Validate_WeakPassword_HasError()
    {
        var command = new RegisterCommand("test@example.com", "testuser", "password");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public async Task Validate_UsernameWithSpecialChars_HasError()
    {
        var command = new RegisterCommand("test@example.com", "test-user", "Password123!");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }
}
