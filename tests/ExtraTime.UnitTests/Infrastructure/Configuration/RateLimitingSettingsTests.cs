using ExtraTime.Infrastructure.Configuration;
using ExtraTime.UnitTests.Attributes;

namespace ExtraTime.UnitTests.Infrastructure.Configuration;

[TestCategory("Significant")]
public sealed class RateLimitingSettingsTests
{
    [Test]
    public async Task Defaults_AreExpected()
    {
        // Act
        var settings = new RateLimitingSettings();

        // Assert
        await Assert.That(settings.Enabled).IsTrue();
        await Assert.That(settings.TokenLimit).IsEqualTo(100);
        await Assert.That(settings.TokensPerPeriod).IsEqualTo(10);
        await Assert.That(settings.ReplenishPeriodSeconds).IsEqualTo(1);
        await Assert.That(settings.QueueLimit).IsEqualTo(0);
        await Assert.That(settings.AutoReplenishment).IsTrue();
    }
}
