using AppForDeploy1.Models;
using AppForDeploy1.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace AppForDeploy1.Tests.UnitTests;

[Trait("Category", "Unit")]
public class UserRepositoryTests
{
    [Fact]
    public async Task DeleteAsync_When_User_Exists_Should_Return_True_And_Remove_From_Database_And_Cache()
    {
        // Arrange
        int userId = 1;
        var user = new User()
        {
            Id = userId,
            Name = "Test",
            Email = "test@gmail.com",
            CreatedAt = DateTime.UtcNow,
        };
        
        var mockSet = new Mock<DbSet<User>>();
        mockSet
            .Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync(user);
        
        var mockContext = new Mock<AppDbContext>();
        mockContext.Setup(c => c.Users).Returns(mockSet.Object);
        mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(userId);

        var mockCache = new Mock<IDistributedCache>();
        mockCache.Setup(c => c.RemoveAsync(It.IsAny<string>(), default)).Returns(Task.CompletedTask);
        
        var repository = new UserRepository(mockContext.Object, mockCache.Object);
        
        // Act
        var result = await repository.DeleteAsync(user.Id);
        
        // Assert
        Assert.True(result);
        mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        mockCache.Verify(c => c.RemoveAsync($"user:{userId}", default), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_When_No_User_Should_Return_False_And_Persist_Cache()
    {
        // Arrange
        var mockSet = new Mock<DbSet<User>>();
        mockSet
            .Setup(s => s.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((User?)null);
        
        var mockContext = new Mock<AppDbContext>();
        mockContext.Setup(c => c.Users).Returns(mockSet.Object);
        
        var mockCache = new Mock<IDistributedCache>();
        var repository = new UserRepository(mockContext.Object, mockCache.Object);
    
        // Act
        var result = await repository.DeleteAsync(1);
    
        // Assert
        Assert.False(result);
        mockContext.Verify(c => c.SaveChangesAsync(default), Times.Never);
        mockCache.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }
}