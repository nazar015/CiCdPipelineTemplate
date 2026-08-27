using System.Text.Json;
using AppForDeploy1.Models;
using AppForDeploy1.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region External Services

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    Console.WriteLine($"[APP LOGS] app uses this db connection string: {connectionString}");
    options.UseNpgsql(connectionString);
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "appForDeploy1";
});

#endregion

#region Injections

builder.Services.AddScoped<IUserRepository, UserRepository>();

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal User API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
}

app.UseHttpsRedirection();

#region Config

using (var scope =  app.Services.CreateScope())
{
    var context =  scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

#endregion

#region Endpoints

app.MapPost("/api/users", async ([FromBody] CreateUserDto dto, [FromServices] IUserRepository repository) =>
{
    var user = new User()
    {
        Name = dto.Name,
        Email = dto.Email,
        CreatedAt = DateTime.UtcNow
    };

    await repository.CreateAsync(user);
    
    return Results.Created($"/api/users/{user.Id}", user);
});

app.MapDelete("/api/users/{id:int}", async ([FromRoute] int id, [FromServices] IUserRepository repository) =>
{
    var isDeleted = await repository.DeleteAsync(id);

    if (!isDeleted)
    {
        return Results.NotFound();
    }
    
    return Results.NoContent();
});

app.MapGet("/api/users/{id:int}", async ([FromRoute] int id, [FromServices] IUserRepository repository) =>
{
    var user = await repository.GetAsync(id);

    if (user == null)
    {
        return Results.NotFound();
    }
    
    return Results.Ok(user);
});

app.MapGet("/api/users", async ([FromQuery] string? searchText, [FromServices] IUserRepository repository) =>
{
    var users = await repository.GetAllAsync(searchText);
    return Results.Ok(users);
});

#endregion

app.Run();