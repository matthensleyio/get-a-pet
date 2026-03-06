using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Api.Engines;
using Api.Orchestrators;
using Api.Repositories;

namespace Api;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices((ctx, services) =>
            {
                services.AddSingleton(_ => new TableServiceClient(ctx.Configuration["AzureWebJobsStorage"]));
                services.AddHttpClient("PetBridge", c => c.Timeout = TimeSpan.FromSeconds(30));

                services.AddScoped<StateRepository>();
                services.AddScoped<DogRepository>();
                services.AddScoped<SubscriptionRepository>();

                services.AddScoped<ScrapingEngine>();
                services.AddScoped<DogDiffEngine>();
                services.AddScoped<NotificationEngine>();

                services.AddScoped<MonitorOrchestrator>();
                services.AddScoped<StatusOrchestrator>();
            })
            .Build();

        var tables = host.Services.GetRequiredService<TableServiceClient>();
        await tables.CreateTableIfNotExistsAsync("Dogs");
        await tables.CreateTableIfNotExistsAsync("SiteState");
        await tables.CreateTableIfNotExistsAsync("PushSubscriptions");

        await host.RunAsync();
    }
}
