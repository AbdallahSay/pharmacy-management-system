using Microsoft.EntityFrameworkCore;
using Pharmacy.Infrastructure.Identity;
using Pharmacy.Infrastructure.Persistence;

namespace Pharmacy.API.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        try
        {
            Console.WriteLine(" Starting Migration...");
            await Console.Out.FlushAsync();

            await using var scope = app.Services.CreateAsyncScope();
            var context = scope.ServiceProvider
                .GetRequiredService<PharmacyDbContext>();

            await context.Database.MigrateAsync();

            Console.WriteLine(" Migration completed successfully!");
            await Console.Out.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(" MIGRATION FAILED ");
            Console.Error.WriteLine(ex.ToString());
            await Console.Error.FlushAsync();

           
            await File.WriteAllTextAsync(
                @"logs\migration_error.txt",
                ex.ToString());

            throw;
        }
    }
    public static async Task SeedIdentityAsync(this WebApplication app)
    {
       
        await IdentityDataSeeder.SeedAsync(app.Services);
    }
}
