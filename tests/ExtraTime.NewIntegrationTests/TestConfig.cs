using ExtraTime.NewIntegrationTests.Factories;
using ExtraTime.NewIntegrationTests.Fixtures;

namespace ExtraTime.NewIntegrationTests;

public static class TestConfig
{
    public static SharedContainerFixture? SqlContainer { get; private set; }
    
    // Default to InMemory if not specified
    public static bool UseSqlServer => Environment.GetEnvironmentVariable("TEST_MODE") == "SqlServer";

    [Before(Assembly)]
    public static async Task InitializeAsync()
    {
        if (UseSqlServer)
        {
            SqlContainer = new SharedContainerFixture();
            await SqlContainer.InitializeAsync();
        }
    }

    [After(Assembly)]
    public static async Task CleanupAsync()
    {
        if (SqlContainer != null)
        {
            await SqlContainer.DisposeAsync();
        }
    }

    public static ITestDatabase CreateDatabase()
    {
        if (UseSqlServer && SqlContainer != null)
        {
            return new SqlServerTestDatabase(SqlContainer.ConnectionString);
        }
        
        return new InMemoryTestDatabase();
    }
}
