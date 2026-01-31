using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using ExtraTime.Infrastructure.Services.Bots;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.MigrationService;

public sealed class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunMigrationsAsync(stoppingToken);
            await SeedDataAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }

    private async Task RunMigrationsAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("Database migrations applied successfully");
    }

    private async Task SeedDataAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = new PasswordHasher();
        var botSeeder = new BotSeeder(dbContext, passwordHasher);

        logger.LogInformation("Seeding default bots...");
        await botSeeder.SeedDefaultBotsAsync(stoppingToken);
        logger.LogInformation("Default bots seeded successfully");
    }
}
