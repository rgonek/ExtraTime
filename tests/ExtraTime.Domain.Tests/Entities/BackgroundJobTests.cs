using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class BackgroundJobTests
{
    [Test]
    public async Task Create_WithValidData_CreatesJob()
    {
        // Arrange
        var jobType = "TestJob";
        var payload = "{\"data\": \"test\"}";

        // Act
        var job = BackgroundJob.Create(jobType, payload);

        // Assert
        await Assert.That(job.JobType).IsEqualTo(jobType);
        await Assert.That(job.Payload).IsEqualTo(payload);
        await Assert.That(job.Status).IsEqualTo(JobStatus.Pending);
        await Assert.That(job.RetryCount).IsEqualTo(0);
        await Assert.That(job.MaxRetries).IsEqualTo(3);
        await Assert.That(job.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(job.DomainEvents.First()).IsTypeOf<JobCreated>();
    }

    [Test]
    public async Task Create_WithAllParameters_CreatesJob()
    {
        // Arrange
        var jobType = "TestJob";
        var payload = "{\"data\": \"test\"}";
        var scheduledAt = DateTime.UtcNow.AddHours(1);
        var createdByUserId = Guid.NewGuid();
        var correlationId = "corr-123";
        var maxRetries = 5;

        // Act
        var job = BackgroundJob.Create(jobType, payload, scheduledAt, createdByUserId, correlationId, maxRetries);

        // Assert
        await Assert.That(job.ScheduledAt).IsEqualTo(scheduledAt);
        await Assert.That(job.CreatedByUserId).IsEqualTo(createdByUserId);
        await Assert.That(job.CorrelationId).IsEqualTo(correlationId);
        await Assert.That(job.MaxRetries).IsEqualTo(maxRetries);
    }

    [Test]
    public async Task Create_WithEmptyJobType_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => BackgroundJob.Create(""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Create_WithWhitespaceJobType_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.That(() => BackgroundJob.Create("   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task MarkAsProcessing_SetsStartedAtAndStatus()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.ClearDomainEvents();

        // Act
        job.MarkAsProcessing();

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Processing);
        await Assert.That(job.StartedAt).IsNotNull();
        await Assert.That(job.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(job.DomainEvents.First()).IsTypeOf<JobStatusChanged>();
    }

    [Test]
    public async Task MarkAsProcessing_FromRetrying_SetsStartedAtAndStatus()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.MarkAsFailed("Error"); // Moves to Retrying
        job.ClearDomainEvents();

        // Act
        job.MarkAsProcessing();

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Processing);
    }

    [Test]
    public async Task MarkAsProcessing_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.MarkAsCompleted();

        // Act & Assert
        await Assert.That(() => job.MarkAsProcessing())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MarkAsProcessing_FromFailed_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.MarkAsFailed("Error");
        // Force to Failed (max retries reached)
        for (int i = 0; i < 2; i++)
        {
            job.MarkAsProcessing();
            job.MarkAsFailed("Error");
        }

        // Act & Assert
        await Assert.That(() => job.MarkAsProcessing())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MarkAsCompleted_SetsCompletedAtAndResult()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.ClearDomainEvents();
        var result = "Success result";

        // Act
        job.MarkAsCompleted(result);

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Completed);
        await Assert.That(job.Result).IsEqualTo(result);
        await Assert.That(job.CompletedAt).IsNotNull();
        await Assert.That(job.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(job.DomainEvents.First()).IsTypeOf<JobStatusChanged>();
    }

    [Test]
    public async Task MarkAsCompleted_WithoutResult_SetsStatus()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();

        // Act
        job.MarkAsCompleted();

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Completed);
        await Assert.That(job.Result).IsNull();
    }

    [Test]
    public async Task MarkAsCompleted_FromPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");

        // Act & Assert
        await Assert.That(() => job.MarkAsCompleted())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MarkAsFailed_SetsErrorAndStatus_ToRetrying()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.ClearDomainEvents();
        var error = "Something went wrong";

        // Act
        job.MarkAsFailed(error);

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Retrying);
        await Assert.That(job.Error).IsEqualTo(error);
        await Assert.That(job.RetryCount).IsEqualTo(1);
        await Assert.That(job.DomainEvents).Count().IsEqualTo(2); // JobRetrying + JobStatusChanged
        await Assert.That(job.DomainEvents.Any(e => e is JobRetrying)).IsTrue();
        await Assert.That(job.DomainEvents.Any(e => e is JobStatusChanged)).IsTrue();
    }

    [Test]
    public async Task MarkAsFailed_MaxRetriesReached_SetsStatusToFailed()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob", maxRetries: 2);
        job.MarkAsProcessing();
        job.MarkAsFailed("Error 1");
        job.MarkAsProcessing();

        // Act
        job.MarkAsFailed("Error 2");

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Failed);
        await Assert.That(job.RetryCount).IsEqualTo(2);
        await Assert.That(job.DomainEvents.Any(e => e is JobFailed)).IsTrue();
    }

    [Test]
    public async Task MarkAsFailed_FromPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");

        // Act & Assert
        await Assert.That(() => job.MarkAsFailed("Error"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Cancel_SetsStatusToCancelled()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.ClearDomainEvents();

        // Act
        job.Cancel();

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(job.CompletedAt).IsNotNull();
        await Assert.That(job.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(job.DomainEvents.First()).IsTypeOf<JobStatusChanged>();
    }

    [Test]
    public async Task Cancel_FromProcessing_SetsStatusToCancelled()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.ClearDomainEvents();

        // Act
        job.Cancel();

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Cancelled);
    }

    [Test]
    public async Task Cancel_FromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.MarkAsCompleted();

        // Act & Assert
        await Assert.That(() => job.Cancel())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Retry_FromFailed_SetsStatusToPending()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob", maxRetries: 2);
        job.MarkAsProcessing();
        job.MarkAsFailed("Error");
        job.MarkAsProcessing();
        job.MarkAsFailed("Error again"); // Now Failed
        job.ClearDomainEvents();

        // Act
        job.Retry();

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Pending);
        await Assert.That(job.Error).IsNull();
        await Assert.That(job.CompletedAt).IsNull();
        await Assert.That(job.RetryCount).IsEqualTo(3);
        await Assert.That(job.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(job.DomainEvents.First()).IsTypeOf<JobStatusChanged>();
    }

    [Test]
    public async Task Retry_FromCancelled_SetsStatusToPending()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.Cancel();
        job.ClearDomainEvents();

        // Act
        job.Retry();

        // Assert
        await Assert.That(job.Status).IsEqualTo(JobStatus.Pending);
    }

    [Test]
    public async Task Retry_FromPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");

        // Act & Assert
        await Assert.That(() => job.Retry())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Retry_FromProcessing_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();

        // Act & Assert
        await Assert.That(() => job.Retry())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task CanBeRetried_WhenFailed_ReturnsTrue()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob", maxRetries: 1);
        job.MarkAsProcessing();
        job.MarkAsFailed("Error");

        // Assert
        await Assert.That(job.CanBeRetried).IsTrue();
    }

    [Test]
    public async Task CanBeRetried_WhenCancelled_ReturnsTrue()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.Cancel();

        // Assert
        await Assert.That(job.CanBeRetried).IsTrue();
    }

    [Test]
    public async Task CanBeRetried_WhenPending_ReturnsFalse()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");

        // Assert
        await Assert.That(job.CanBeRetried).IsFalse();
    }

    [Test]
    public async Task IsTerminalStatus_WhenCompleted_ReturnsTrue()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();
        job.MarkAsCompleted();

        // Assert
        await Assert.That(job.IsTerminalStatus).IsTrue();
    }

    [Test]
    public async Task IsTerminalStatus_WhenFailed_ReturnsTrue()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob", maxRetries: 1);
        job.MarkAsProcessing();
        job.MarkAsFailed("Error");

        // Assert
        await Assert.That(job.IsTerminalStatus).IsTrue();
    }

    [Test]
    public async Task IsTerminalStatus_WhenCancelled_ReturnsTrue()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.Cancel();

        // Assert
        await Assert.That(job.IsTerminalStatus).IsTrue();
    }

    [Test]
    public async Task IsTerminalStatus_WhenPending_ReturnsFalse()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");

        // Assert
        await Assert.That(job.IsTerminalStatus).IsFalse();
    }

    [Test]
    public async Task IsTerminalStatus_WhenProcessing_ReturnsFalse()
    {
        // Arrange
        var job = BackgroundJob.Create("TestJob");
        job.MarkAsProcessing();

        // Assert
        await Assert.That(job.IsTerminalStatus).IsFalse();
    }
}
