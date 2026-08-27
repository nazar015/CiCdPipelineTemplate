using AppForDeploy1.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Abstractions;

namespace AppForDeploy1.Tests.IntegrationTests;

public class IntegrationWebApplicationFactory: WebApplicationFactory<Program>
{
    private readonly string _postgresConnectionString;
    private readonly string _redisConnectionString;
    
    public IntegrationWebApplicationFactory(string postgresConnectionString, string redisConnectionString)
    {
        _postgresConnectionString = postgresConnectionString ?? throw new ArgumentException("Postgres connection string is empty");
        _redisConnectionString = redisConnectionString ?? throw new ArgumentException("Redis connection string is empty");
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            #region Remove Local Dependencies

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDistributedCache>();

            #endregion

            
            #region Add Containerized Dependencies

            services.AddDbContext<AppDbContext>(options => { options.UseNpgsql(_postgresConnectionString); });
            services.AddStackExchangeRedisCache(options => { options.Configuration = _redisConnectionString; });

            #endregion
        });
        
        builder.ConfigureServices(services =>
        {
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            
            if (db.Users.Any())
            {
                db.Users.AddRange(
                    new User { Name = "Alice", Email = "alice@test.com", CreatedAt = DateTime.UtcNow },
                    new User { Name = "Bob", Email = "bob@test.com", CreatedAt = DateTime.UtcNow }
                );
                db.SaveChanges();
            }
        });
    }
}