using ExtraTime.Domain.Entities;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Infrastructure.Data;

public sealed class ApplicationDbContextTests : IntegrationTestBase
{
    [Test]
    public async Task SaveChangesAsync_ShouldPopulateAuditProperties()
    {
        // Arrange
        var user = new UserBuilder().Build();

        // Act
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Assert
        await Assert.That(user.CreatedAt).IsGreaterThan(DateTime.MinValue);
        await Assert.That(user.UpdatedAt!.Value).IsEqualTo(user.CreatedAt).Within(TimeSpan.FromSeconds(1));

        // Act - Update
        var originalCreatedAt = user.CreatedAt;
        user.Username = "updated_username";
        await Task.Delay(200); // Ensure some time passes
        await Context.SaveChangesAsync();

        // Assert
        await Assert.That(user.CreatedAt).IsEqualTo(originalCreatedAt).Within(TimeSpan.FromSeconds(1));
        await Assert.That(user.UpdatedAt!.Value).IsGreaterThan(originalCreatedAt);
    }
}
