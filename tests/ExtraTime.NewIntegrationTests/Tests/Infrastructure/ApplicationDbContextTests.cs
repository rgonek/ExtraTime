using ExtraTime.Domain.Entities;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.NewIntegrationTests.Tests.Infrastructure;

public sealed class ApplicationDbContextTests : NewIntegrationTestBase
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
        
        // Note: In current implementation, UpdatedAt might be null or same as CreatedAt depending on how it's handled.
        // User class has default values in some builders.
        
        // Act - Update
        var originalCreatedAt = user.CreatedAt;
        user.UpdateProfile(user.Email, "updated_username");
        await Task.Delay(200); 
        await Context.SaveChangesAsync();

        // Assert
        await Assert.That(user.CreatedAt).IsEqualTo(originalCreatedAt).Within(TimeSpan.FromSeconds(1));
        await Assert.That(user.UpdatedAt).IsNotNull();
        await Assert.That(user.UpdatedAt!.Value).IsGreaterThan(originalCreatedAt);
    }
}
