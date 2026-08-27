using Microsoft.EntityFrameworkCore;

namespace AppForDeploy1.Models;

public class AppDbContext: DbContext
{
    public virtual DbSet<User> Users { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    protected AppDbContext() { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        
        modelBuilder.Entity<User>().Property(u => u.Id).ValueGeneratedOnAdd();

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}