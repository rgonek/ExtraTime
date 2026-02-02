using Testcontainers.MsSql;

namespace ExtraTime.NewIntegrationTests.Fixtures;

public class SharedContainerFixture : IAsyncDisposable
{
    private readonly MsSqlContainer _container;
    
    public SharedContainerFixture()
    {
        // Using constructor with image as recommended
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
