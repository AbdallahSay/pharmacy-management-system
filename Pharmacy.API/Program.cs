using Pharmacy.API.Extensions;
using Pharmacy.Infrastructure;
using Scalar.AspNetCore;

namespace Pharmacy.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddOpenApi("Pharmacy");

        var app = builder.Build();

        await app.ApplyMigrationsAsync();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => options.Title = "Pharmacy API");
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync();
    }
}
