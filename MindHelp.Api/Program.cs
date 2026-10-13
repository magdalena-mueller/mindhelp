using Microsoft.EntityFrameworkCore;

namespace MindHelp.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // var connectionString = builder.Configuration.GetConnectionString("Supabase");
        // if (string.IsNullOrWhiteSpace(connectionString))
        // {
        //     throw new InvalidOperationException(
        //         "Die Supabase-Verbindung fehlt. Setze ConnectionStrings__Supabase.");
        // }

        // builder.Services.AddDbContext<MindHelpDbContext>(options =>
        //     options.UseNpgsql(connectionString));

        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}
