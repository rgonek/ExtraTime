using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Bots.DTOs;
using Mediator;

namespace ExtraTime.Application.Features.Bots.Queries.GetBotConfigurationPresets;

public sealed record GetBotConfigurationPresetsQuery : IRequest<Result<List<ConfigurationPresetDto>>>;
