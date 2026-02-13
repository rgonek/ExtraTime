using ExtraTime.Infrastructure.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace ExtraTime.Functions.Activities;

public sealed class GetCompetitionIdsActivity(IOptions<FootballDataSettings> settings)
{
    [Function(nameof(GetCompetitionIdsActivity))]
    public List<int> Run([ActivityTrigger] object? _)
    {
        return settings.Value.SupportedCompetitionIds.ToList();
    }
}
