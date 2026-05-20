using Microsoft.EntityFrameworkCore;
using Pharmacy.Infrastructure.Identity;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.API.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        //if (!app.Environment.IsDevelopment())
        //    return;

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
        await context.Database.MigrateAsync();
    }

    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        await IdentityDataSeeder.SeedAsync(app.Services);
    }
}
