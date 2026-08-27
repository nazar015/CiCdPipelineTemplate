using AppForDeploy1.Models;

namespace AppForDeploy1.Repositories;

public interface IUserRepository
{
    Task CreateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<User?> GetAsync(int id);
    Task<List<User>> GetAllAsync(string? searchText = null);
}