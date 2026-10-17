using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using static TaskManager.Domain.Enums.Enums;

namespace TaskManager.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        var email = config["Seed:AdminEmail"] ?? "admin@taskmanager.local";
        var password = config["Seed:AdminPassword"] ?? "Admin@12345";

        // IgnoreQueryFilters: a soft-deleted admin row must not cause a duplicate insert
        var exists = await db.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.NormalizedEmail == User.NormalizeEmail(email));
        if (exists) return;

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<TimeProvider>();

        db.Users.Add(User.Create("Administrator", email, hasher.Hash(password),
            UserRole.Admin, clock.GetUtcNow().UtcDateTime));
        await db.SaveChangesAsync();
    }
}