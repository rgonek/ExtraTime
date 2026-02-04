using Testcontainers.MsSql;

namespace ExtraTime.IntegrationTests.Fixtures;

public class SharedContainerFixture : IAsyncDisposable
{
    private readonly MsSqlContainer _container;

    public SharedContainerFixture()
    {
        // Suppress obsolete warning as we configure image explicitly
#pragma warning disable CS0618
        _container = new MsSqlBuilder()
#pragma warning restore CS0618
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithReuse(true)
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
