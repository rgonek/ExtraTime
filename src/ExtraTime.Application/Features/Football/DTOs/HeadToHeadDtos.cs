namespace ExtraTime.Application.Features.Football.DTOs;

public sealed record HeadToHeadDto(
    Guid Team1Id,
    string Team1Name,
    Guid Team2Id,
    string Team2Name,
    int TotalMatches,
    int Team1Wins,
    int Team2Wins,
    int Draws,
    int Team1Goals,
    int Team2Goals,
    double BttsRate,
    double Over25Rate,
    int RecentTeam1Wins,
    int RecentTeam2Wins,
    int RecentDraws,
    DateTime? LastMatchDate,
    DateTime CalculatedAt);
