using FluentValidation;
using FluentValidation.TestHelper;

namespace ExtraTime.UnitTests.Common;

public abstract class ValidatorTestBase<TValidator, TCommand>
    where TValidator : AbstractValidator<TCommand>, new()
{
    protected readonly TValidator Validator = new();

    protected void ShouldHaveValidationErrorFor(TCommand command, string propertyName)
    {
        var result = Validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(propertyName);
    }

    protected void ShouldNotHaveValidationErrorFor(TCommand command, string propertyName)
    {
        var result = Validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(propertyName);
    }
}
