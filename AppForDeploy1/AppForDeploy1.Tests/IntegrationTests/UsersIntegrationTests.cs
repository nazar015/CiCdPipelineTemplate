using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AppForDeploy1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace AppForDeploy1.Tests.IntegrationTests;

[Trait("Category", "Integration")]
public class UsersIntegrationTests: IClassFixture<TestcontainersFixture>
{
    private readonly HttpClient _httpClient;
    private readonly IntegrationWebApplicationFactory _factory;

    public UsersIntegrationTests(TestcontainersFixture fixture)
    {
        _factory = new IntegrationWebApplicationFactory(fixture.Postgres.GetConnectionString(), fixture.Redis.GetConnectionString());
        _httpClient = _factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateUser_Should_Persist_In_Postgres()
    {
        // Arrange
        var newUser = new User()
        {
            Name = "test1",
            Email = "test1@test.com",
            CreatedAt = DateTime.UtcNow
        };
        
        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/users", newUser);

        // Assert (http)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var userFromApi = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(userFromApi);
        Assert.True(userFromApi.Id > 0);
        Assert.Equal(newUser.Name, userFromApi.Name);
        Assert.Equal(newUser.Email, userFromApi.Email);

        // Assert (db)
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
       
        var userInDb = await context.Users.FindAsync(userFromApi.Id);
        Assert.NotNull(userInDb);
        Assert.Equal(userInDb.Id, userFromApi.Id);
        Assert.Equal(newUser.Name, userInDb.Name);
        Assert.Equal(newUser.Email, userInDb.Email);
    }
    
    [Fact]
    public async Task GetUser_Should_Read_From_Postgres_Redis()
    {
        // Arrange
        var createResponse = await _httpClient.PostAsJsonAsync("/api/users", new
        {
            Name = "db and cache test",
            Email = "db.cache@test.com"
        });
        var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
        var userId = createdUser!.Id;
        var cacheKey = $"user:{userId}";
        
        // Act
        var response = await _httpClient.GetAsync($"/api/users/{userId}");

        // Assert (http)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        // Assert (db)
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var userInDb = await context.Users.FindAsync(userId);
        Assert.NotNull(userInDb);
        
        // Act (cache)
        var newResponse = await _httpClient.GetAsync($"/api/users/{userId}");
        
        // Assert (cache)
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var userInCache = await cache.GetStringAsync(cacheKey);
        Assert.NotNull(userInCache);
        
        var userFromCache = JsonSerializer.Deserialize<User>(userInCache);
        Assert.NotNull(userFromCache);
        Assert.Equal(userInDb.Name, userFromCache.Name);
        Assert.Equal(userInDb.Email, userFromCache.Email);
        Assert.Equal(userInDb.CreatedAt, userFromCache.CreatedAt);
    }
}