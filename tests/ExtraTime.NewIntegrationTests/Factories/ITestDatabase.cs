using ExtraTime.Infrastructure.Data;

namespace ExtraTime.NewIntegrationTests.Factories;

public interface ITestDatabase : IAsyncDisposable
{
    Task InitializeAsync();
    ApplicationDbContext CreateContext();
    string ConnectionString { get; }
}
