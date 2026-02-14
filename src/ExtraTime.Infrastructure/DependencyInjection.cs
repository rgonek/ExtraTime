using System.Linq;
using System.Text;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Application.Features.Bots.Strategies;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using ExtraTime.Infrastructure.Services.Bots;
using ExtraTime.Infrastructure.Services.Football;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

        // Bot Services
        services.AddScoped<BotSeeder>();
        services.AddScoped<ITeamFormCalculator, TeamFormCalculator>();
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
