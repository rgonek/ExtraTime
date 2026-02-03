using System.Text.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.UnitTests.Attributes;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory(TestCategories.Significant)]
public sealed class InMemoryJobDispatcherTests
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InMemoryJobDispatcher> _logger;
    private readonly InMemoryJobDispatcher _dispatcher;
    private readonly CancellationToken _ct = CancellationToken.None;

    public InMemoryJobDispatcherTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _logger = Substitute.For<ILogger<InMemoryJobDispatcher>>();
        _dispatcher = new InMemoryJobDispatcher(_context, _logger);
    }

    [Test]
    public async Task EnqueueAsync_CreatesJobRecord()
    {
        // Arrange
        var jobType = "TestJob";
        var payload = new { Data = "test" };
        BackgroundJob? capturedJob = null;

        _context.BackgroundJobs.Add(Arg.Do<BackgroundJob>(job => capturedJob = job));
        _context.SaveChangesAsync(_ct).Returns(1);

        // Act
        var jobId = await _dispatcher.EnqueueAsync(jobType, payload, _ct);

        // Assert
        await Assert.That(jobId).IsNotEqualTo(Guid.Empty);
        await Assert.That(capturedJob).IsNotNull();
        await Assert.That(capturedJob!.JobType).IsEqualTo(jobType);
        await Assert.That(capturedJob.Status).IsEqualTo(JobStatus.Pending);
    }

    [Test]
    public async Task EnqueueAsync_SavesToDatabase()
    {
        // Arrange
        var jobType = "CalculateBetResults";
        var payload = new { MatchId = Guid.NewGuid(), CompetitionId = Guid.NewGuid() };

        _context.BackgroundJobs.Add(Arg.Any<BackgroundJob>());
        _context.SaveChangesAsync(_ct).Returns(1);

        // Act
        var jobId = await _dispatcher.EnqueueAsync(jobType, payload, _ct);

        // Assert
        _context.BackgroundJobs.Received(1).Add(Arg.Is<BackgroundJob>(j =>
            j.JobType == jobType &&
            j.Status == JobStatus.Pending));
        await _context.Received(1).SaveChangesAsync(_ct);
    }

    [Test]
    public async Task EnqueueAsync_SerializesPayload()
    {
        // Arrange
        var jobType = "ProcessData";
        var payload = new { Id = 123, Name = "Test" };
        BackgroundJob? capturedJob = null;

        _context.BackgroundJobs.Add(Arg.Do<BackgroundJob>(job => capturedJob = job));
        _context.SaveChangesAsync(_ct).Returns(1);

        // Act
        await _dispatcher.EnqueueAsync(jobType, payload, _ct);

        // Assert
        await Assert.That(capturedJob).IsNotNull();
        var deserializedPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(capturedJob!.Payload!);
        await Assert.That(deserializedPayload).IsNotNull();
    }

    [Test]
    public async Task DispatchAsync_ReturnsCompletedTask()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob", "{}");

        // Act
        var task = _dispatcher.DispatchAsync(job, _ct);

        // Assert
        await Assert.That(task.IsCompleted).IsTrue();
    }
}
