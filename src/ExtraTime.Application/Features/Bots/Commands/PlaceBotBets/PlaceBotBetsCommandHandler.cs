using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bots.Services;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Commands.PlaceBotBets;

public sealed class PlaceBotBetsCommandHandler(
    IBotBettingService botService) : IRequestHandler<PlaceBotBetsCommand, Result<int>>
{
    public async ValueTask<Result<int>> Handle(PlaceBotBetsCommand request, CancellationToken ct)
    {
        var betsPlaced = await botService.PlaceBetsForUpcomingMatchesAsync(ct);
        return Result<int>.Success(betsPlaced);
    }
}
