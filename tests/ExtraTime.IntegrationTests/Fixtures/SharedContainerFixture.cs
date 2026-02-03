using Testcontainers.MsSql;

namespace ExtraTime.IntegrationTests.Fixtures;

public class SharedContainerFixture : IAsyncDisposable
{
    private readonly MsSqlContainer _container;

    public SharedContainerFixture()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
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
