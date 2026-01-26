using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bets;
using ExtraTime.Application.Features.Bets.Commands.PlaceBet;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bets.Handlers;

public sealed class PlaceBetCommandHandlerTests : HandlerTestBase
{
    private readonly PlaceBetCommandHandler _handler;

    public PlaceBetCommandHandlerTests()
    {
        _handler = new PlaceBetCommandHandler(Context, CurrentUserService);
    }

    [Test]
    public async Task Handle_ValidNewBet_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);

        var league = new LeagueBuilder().Build();
        var match = new MatchBuilder()
            .WithMatchDate(DateTime.UtcNow.AddHours(2))
            .WithStatus(MatchStatus.Scheduled)
            .Build();

        var command = new PlaceBetCommand(league.Id, match.Id, 2, 1);

        var mockLeagues = CreateMockDbSet(new List<League> { league }.AsQueryable());
        Context.Leagues.Returns(mockLeagues);
        
        var mockMembers = CreateMockDbSet(new List<LeagueMember> { 
            new LeagueMember { LeagueId = league.Id, UserId = userId } 
        }.AsQueryable());
        Context.LeagueMembers.Returns(mockMembers);
        
        var mockMatches = CreateMockDbSet(new List<Match> { match }.AsQueryable());
        Context.Matches.Returns(mockMatches);
        
        var mockBets = CreateMockDbSet(new List<Bet>().AsQueryable());
        Context.Bets.Returns(mockBets);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        Context.Bets.Received(1).Add(Arg.Is<Bet>(b => 
            b.LeagueId == league.Id && 
            b.UserId == userId && 
            b.MatchId == match.Id &&
            b.PredictedHomeScore == 2));
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_DeadlinePassed_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);

        var league = new LeagueBuilder().WithScoringRules(3, 1).Build();
        var match = new MatchBuilder()
            .WithMatchDate(DateTime.UtcNow.AddMinutes(2)) // League default deadline is 5 mins
            .WithStatus(MatchStatus.Scheduled)
            .Build();

        var command = new PlaceBetCommand(league.Id, match.Id, 2, 1);

        var mockLeagues = CreateMockDbSet(new List<League> { league }.AsQueryable());
        Context.Leagues.Returns(mockLeagues);
        
        var mockMembers = CreateMockDbSet(new List<LeagueMember> { 
            new LeagueMember { LeagueId = league.Id, UserId = userId } 
        }.AsQueryable());
        Context.LeagueMembers.Returns(mockMembers);
        
        var mockMatches = CreateMockDbSet(new List<Match> { match }.AsQueryable());
        Context.Matches.Returns(mockMatches);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(BetErrors.DeadlinePassed);
    }
}
