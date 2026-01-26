using ExtraTime.Infrastructure.Services;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Test]
    public async Task Hash_ReturnsDifferentString()
    {
        // Arrange
        var password = "StrongPassword123!";

        // Act
        var hash = _hasher.Hash(password);

        // Assert
        await Assert.That(hash).IsNotEqualTo(password);
        await Assert.That(hash.Length).IsGreaterThan(20);
    }

    [Test]
    public async Task Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "StrongPassword123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(password, hash);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Verify_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "StrongPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _hasher.Hash(password);

        // Act
        var result = _hasher.Verify(wrongPassword, hash);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task Hash_SaltsPasswords()
    {
        // Arrange
        var password = "SamePassword";

        // Act
        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        // Assert
        await Assert.That(hash1).IsNotEqualTo(hash2);
    }
}
