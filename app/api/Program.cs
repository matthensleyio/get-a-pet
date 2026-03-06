using Microsoft.EntityFrameworkCore;

using Api.BackgroundServices;
using Api.Engines;
using Api.Orchestrators;
using Api.Repositories;

namespace Api;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddDbContext<DogMonitorDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));

        builder.Services.AddScoped<StateRepository>();
        builder.Services.AddScoped<DogRepository>();
        builder.Services.AddScoped<SubscriptionRepository>();

        builder.Services.AddScoped<ScrapingEngine>();
        builder.Services.AddScoped<DogDiffEngine>();
        builder.Services.AddScoped<NotificationEngine>();

        builder.Services.AddScoped<MonitorOrchestrator>();
        builder.Services.AddScoped<StatusOrchestrator>();

        builder.Services.AddHttpClient("PetBridge", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddHostedService<DogMonitorBackgroundService>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DogMonitorDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        app.UseCors();
        app.MapControllers();

        await app.RunAsync();
    }
}
