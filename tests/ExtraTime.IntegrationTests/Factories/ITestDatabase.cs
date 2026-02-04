using ExtraTime.Infrastructure.Data;

namespace ExtraTime.IntegrationTests.Factories;

public interface ITestDatabase : IAsyncDisposable
{
    Task InitializeAsync();
    Task ResetAsync();
    ApplicationDbContext CreateContext();
    string ConnectionString { get; }
}

