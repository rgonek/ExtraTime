using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.ML.Services;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using ExtraTime.Infrastructure.Services.Bots;
using ExtraTime.Infrastructure.Services.ExternalData;
using ExtraTime.Infrastructure.Services.Football;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Refit;

namespace ExtraTime.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database - only register if not already registered (e.g., by Aspire)
        var dbContextAlreadyRegistered = services.Any(s =>
            s.ServiceType == typeof(ApplicationDbContext) ||
            s.ImplementationType == typeof(ApplicationDbContext));
        if (!dbContextAlreadyRegistered)
        {
            if (configuration.GetValue<bool>("UseInMemoryDatabase"))
            {
                var dbName = configuration["InMemoryDatabaseName"] ?? "ExtraTimeDb";
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                           .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
            }
            else
            {
                var connectionString = configuration.GetConnectionString("extratime");
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));
            }
        }
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // JWT Settings - only configure if JWT section exists (not needed for background jobs)
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        if (jwtSettings != null)
        {
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

            // Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("UserOrAdmin", policy =>
                    policy.RequireRole("User", "Admin"));
            });
        }

        var rateLimitingSection = configuration.GetSection(RateLimitingSettings.SectionName);
        services.Configure<RateLimitingSettings>(rateLimitingSection);
        var rateLimitingSettings = rateLimitingSection.Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        services.AddRateLimiter(options =>
        {
            if (!rateLimitingSettings.Enabled)
            {
                return;
            }

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Path.StartsWithSegments("/alive", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Path.StartsWithSegments("/ready", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetNoLimiter("health-check");
                }

                var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                             context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var partitionKey = !string.IsNullOrWhiteSpace(userId)
                    ? $"user:{userId}"
                    : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = rateLimitingSettings.TokenLimit,
                    TokensPerPeriod = rateLimitingSettings.TokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimitingSettings.ReplenishPeriodSeconds),
                    AutoReplenishment = rateLimitingSettings.AutoReplenishment,
                    QueueLimit = rateLimitingSettings.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many requests. Please try again later." },
                    cancellationToken);
            };
        });

        // Services
        services.AddHttpContextAccessor();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJobDispatcher, InMemoryJobDispatcher>();
        services.AddSingleton<IInviteCodeGenerator, InviteCodeGenerator>();
        services.AddScoped<IBetCalculator, BetCalculator>();
        services.AddScoped<IStandingsCalculator, StandingsCalculator>();
        services.AddScoped<IBetResultsService, BetResultsService>();
        services.AddScoped<IIntegrationHealthService, IntegrationHealthService>();
        services.AddScoped<IUnderstatService, UnderstatService>();
        services.AddScoped<IExternalDataBackfillService, ExternalDataBackfillService>();
        services.AddScoped<IEloRatingService, EloRatingService>();
        services.AddScoped<IInjuryService, InjuryService>();
        services.AddScoped<ISuspensionService, SuspensionService>();
        services.AddScoped<IFplInjuryStatusProvider, FplInjuryStatusProvider>();
        services.AddScoped<ILineupDataProvider, ApiLineupDataProvider>();
        services.AddScoped<ILineupSyncService, LineupSyncService>();
        services.AddScoped<ITeamUsualLineupService, TeamUsualLineupService>();
        services.Configure<UnderstatSettings>(configuration.GetSection(UnderstatSettings.SectionName));
        services.Configure<FootballDataUkSettings>(configuration.GetSection(FootballDataUkSettings.SectionName));
        services.Configure<ClubEloSettings>(configuration.GetSection(ClubEloSettings.SectionName));
        services.Configure<ApiFootballSettings>(configuration.GetSection(ApiFootballSettings.SectionName));
        services.AddHttpClient("Understat", client =>
        {
            client.BaseAddress = new Uri("https://understat.com");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ExtraTime/1.0");
        });
        services.AddHttpClient("FootballDataUk", client =>
        {
            client.BaseAddress = new Uri("https://www.football-data.co.uk");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ExtraTime/1.0");
        });
        services.AddHttpClient("ClubElo", client =>
        {
            client.BaseAddress = new Uri("http://api.clubelo.com");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ExtraTime/1.0");
        });
        services.AddHttpClient("ApiFootball", client =>
        {
            client.BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ExtraTime/1.0");
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "api-football-v1.p.rapidapi.com");
        });
        services.AddHttpClient("Fpl", client =>
        {
            client.BaseAddress = new Uri("https://fantasy.premierleague.com");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ExtraTime/1.0");
        });
        services.AddScoped<IOddsDataService, OddsDataService>();
        services.AddHostedService<UnderstatSyncBackgroundService>();
        services.AddHostedService<OddsSyncBackgroundService>();
        services.AddHostedService<EloSyncBackgroundService>();

        // Bot Services
        services.AddScoped<BotSeeder>();
        services.AddScoped<ITeamFormCalculator, TeamFormCalculator>();
        services.AddScoped<IHeadToHeadService, HeadToHeadService>();
        services.AddScoped<IMlFeatureExtractor, MlFeatureExtractor>();
        services.AddScoped<IMlPredictionService, MlPredictionService>();
        services.AddScoped<BotStrategyFactory>();
        services.AddScoped<IBotBettingService, BotBettingService>();
        // Background services removed - Hangfire handles recurring jobs in production
        // For development, use Hangfire dashboard or API endpoints to trigger jobs manually

        // Football Data Services
        services.Configure<FootballDataSettings>(configuration.GetSection(FootballDataSettings.SectionName));
        services.AddTransient<RateLimitingHandler>();
        services.AddRefitClient<IFootballDataApi>()
            .ConfigureHttpClient(client =>
            {
                var footballSettings = configuration.GetSection(FootballDataSettings.SectionName).Get<FootballDataSettings>();
                client.BaseAddress = new Uri(footballSettings?.BaseUrl ?? "https://api.football-data.org/v4/");
                client.DefaultRequestHeaders.Add("X-Auth-Token", footballSettings?.ApiKey ?? string.Empty);
            })
            .AddHttpMessageHandler<RateLimitingHandler>();
        services.AddScoped<IFootballDataService, FootballDataService>();
        services.AddScoped<IFootballSyncService, FootballSyncService>();
        // services.AddHostedService<FootballSyncHostedService>();

        return services;
    }
}
