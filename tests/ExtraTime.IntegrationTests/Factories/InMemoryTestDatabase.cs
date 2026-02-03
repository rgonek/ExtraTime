using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Factories;

public class InMemoryTestDatabase : ITestDatabase
{
    private readonly ApplicationDbContext _context;
    private readonly string _dbName = Guid.NewGuid().ToString();

    public InMemoryTestDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(
            options,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IMediator>());
    }

    public string ConnectionString => _dbName;

    public ApplicationDbContext CreateContext() => _context;

    public async Task InitializeAsync()
    {
        // Not strictly necessary for InMemory but consistent interface
        await _context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
