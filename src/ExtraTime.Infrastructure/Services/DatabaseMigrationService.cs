using ExtraTime.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services;

public sealed class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseMigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get DbContextOptions for creating a migration-only context
            var options = serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

            // Create ApplicationDbContext without scoped dependencies (they're now optional)
            using var dbContext = new ApplicationDbContext(options);

            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
