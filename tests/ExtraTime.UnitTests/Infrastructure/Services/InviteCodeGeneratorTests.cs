using ExtraTime.Infrastructure.Services;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class InviteCodeGeneratorTests
{
    private readonly InviteCodeGenerator _generator = new();

    [Test]
    public async Task Generate_ReturnsCorrectLength()
    {
        // Act
        var code = _generator.Generate();

        // Assert
        await Assert.That(code.Length).IsEqualTo(8);
    }

    [Test]
    public async Task Generate_UsesAllowedCharactersOnly()
    {
        // Arrange
        const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        // Act
        var code = _generator.Generate();

        // Assert
        foreach (var c in code)
        {
            await Assert.That(allowed.Contains(c)).IsTrue();
        }
    }

    [Test]
    public async Task GenerateUniqueAsync_ReturnsUniqueCode()
    {
        // Arrange
        var attempt = 0;
        // Mock exists check to return true first two times, then false
        Task<bool> ExistsCheck(string code, CancellationToken ct) => Task.FromResult(++attempt <= 2);

        // Act
        var code = await _generator.GenerateUniqueAsync(ExistsCheck);

        // Assert
        await Assert.That(code).IsNotNull();
        await Assert.That(attempt).IsEqualTo(3);
    }

    [Test]
    public async Task GenerateUniqueAsync_ThrowsAfterMaxAttempts()
    {
        // Arrange
        Task<bool> AlwaysExists(string code, CancellationToken ct) => Task.FromResult(true);

        // Act & Assert
        await Assert.That(async () => await _generator.GenerateUniqueAsync(AlwaysExists))
            .Throws<InvalidOperationException>();
    }
}
