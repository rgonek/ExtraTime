using ExtraTime.Application.Features.Auth.Commands.Login;
using ExtraTime.UnitTests.Common;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Application.Features.Auth.Validators;

public sealed class LoginCommandValidatorTests : ValidatorTestBase<LoginCommandValidator, LoginCommand>
{
    [Test]
    public async Task Validate_ValidCommand_HasNoErrors()
    {
        var command = new LoginCommand("test@example.com", "Password123!");
        var result = await Validator.TestValidateAsync(command);
        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyEmail_HasError()
    {
        var command = new LoginCommand("", "Password123!");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public async Task Validate_InvalidEmail_HasError()
    {
        var command = new LoginCommand("invalid-email", "Password123!");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public async Task Validate_EmptyPassword_HasError()
    {
        var command = new LoginCommand("test@example.com", "");
        var result = await Validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
