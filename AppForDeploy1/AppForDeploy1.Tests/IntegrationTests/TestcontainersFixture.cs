using AppForDeploy1.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace AppForDeploy1.Tests.IntegrationTests;

public class TestcontainersFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = null!;
    public RedisContainer Redis { get; private set; } = null!;
    
    public async Task InitializeAsync()
    {
        // using builders maps a random host port → container port and gives ready-to-use connection string
        Postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        Redis = new RedisBuilder("redis:7-alpine")
            .WithName("testcache")
            .WithCleanUp(true)
            .Build();
        
        await Task.WhenAll(Postgres.StartAsync(), Redis.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(Postgres.StopAsync(), Redis.StopAsync());
    }
}