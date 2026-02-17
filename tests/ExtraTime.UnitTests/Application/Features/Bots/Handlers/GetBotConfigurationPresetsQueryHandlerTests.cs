using ExtraTime.Application.Features.Bots.Queries.GetBotConfigurationPresets;

namespace ExtraTime.UnitTests.Application.Features.Bots.Handlers;

public sealed class GetBotConfigurationPresetsQueryHandlerTests
{
    [Test]
    public async Task Handle_ReturnsExpectedPresets()
    {
        var handler = new GetBotConfigurationPresetsQueryHandler();

        var result = await handler.Handle(new GetBotConfigurationPresetsQuery(), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(10);
        await Assert.That(result.Value.Any(p => p.Name == "Full Analysis")).IsTrue();
        await Assert.That(result.Value.Any(p => p.Name == "xG Expert")).IsTrue();
        await Assert.That(result.Value.Any(p => p.Name == "Market Follower")).IsTrue();
        await Assert.That(result.Value.Any(p => p.Name == "Injury Aware")).IsTrue();
    }
}
