using System.Text.Json.Serialization;
using Azure.Identity;
using ExtraTime.Application;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Infrastructure;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using ExtraTime.API.Features.Admin;
using ExtraTime.API.Features.Auth;
using ExtraTime.API.Features.Bots;
using ExtraTime.API.Features.Football;
using ExtraTime.API.Features.Health;
using ExtraTime.API.Features.Leagues;
using ExtraTime.API.Features.Bets;
using ExtraTime.API.Features.BackgroundJobs;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault configuration in Production
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVault:Name"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }
}

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add Aspire SQL Server DbContext integration (when running under Aspire)
// Falls back to connection string from configuration when not running under Aspire
builder.AddSqlServerDbContext<ApplicationDbContext>("extratime");

// Configure JSON serialization - enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Clean Architecture services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Hangfire background jobs
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

// Add Hangfire server (processes jobs) - minimize worker count for free tier
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1;
});

// Run database migrations on startup in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DatabaseMigrationService>();
}

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ExtraTime API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            []
        }
    });
});

// Note: Health checks are configured by Aspire service defaults and AddSqlServerDbContext

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (protected - admin only in production)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

    // Register recurring jobs (only in non-development to avoid conflicts with Aspire)
    if (!app.Environment.IsDevelopment())
    {
        // Sync football matches every hour
        RecurringJob.AddOrUpdate<IFootballSyncService>(
            "sync-matches",
            x => x.SyncMatchesAsync(null, null, CancellationToken.None),
            Cron.Hourly);

        // Calculate bet results every 15 minutes
        // Using a background job that sends a mediator command
        RecurringJob.AddOrUpdate(
            "calculate-bet-results",
            () => Console.WriteLine("Bet results calculation triggered - implement with mediator"),
            "*/15 * * * *");

        // Bot betting daily at midnight
        RecurringJob.AddOrUpdate<IBotBettingService>(
            "bot-betting",
            x => x.PlaceBetsForUpcomingMatchesAsync(CancellationToken.None),
            Cron.Daily);
    }

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map endpoints
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapFootballEndpoints();
app.MapFootballSyncEndpoints();
app.MapLeagueEndpoints();
app.MapBotEndpoints();
app.MapAdminBotEndpoints();
app.MapBetEndpoints();

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
