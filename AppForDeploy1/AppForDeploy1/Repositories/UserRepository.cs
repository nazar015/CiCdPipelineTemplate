using System.Text.Json;
using AppForDeploy1.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace AppForDeploy1.Repositories;

public class UserRepository(AppDbContext context, IDistributedCache cache) : IUserRepository
{
    public async Task CreateAsync(User user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user =  await context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }
        
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        await cache.RemoveAsync($"user:{id}");

        return true;
    }

    public async Task<User?> GetAsync(int id)
    {
        var cacheKey = $"user:{id}";
    
        var cached = await cache.GetStringAsync(cacheKey);

        if (cached != null)
        {
            var result = JsonSerializer.Deserialize<User>(cached);
            return  result;
        }
    
        var user =  await context.Users.FindAsync(id);

        if (user is null)
        {
            return null;
        }

        var options = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };
    
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), options);

        return user;
    }

    public async Task<List<User>> GetAllAsync(string? searchText = null)
    {
        var query = context.Users.AsQueryable();
    
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            searchText = searchText.ToLower();
            query =  query.Where(u => 
                u.Name.ToLower().Contains(searchText) ||
                u.Email.ToLower().Contains(searchText));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        return users;
    }
}