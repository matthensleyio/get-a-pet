using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Api.DomainModels;
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
                var connectionString = ctx.Configuration["AzureWebJobsStorage"]
                    ?? ctx.Configuration["STORAGE_CONNECTION_STRING"];
                services.AddSingleton(_ => new TableServiceClient(connectionString));
                services.AddHttpClient("PetBridge", c => c.Timeout = TimeSpan.FromSeconds(30));

                IReadOnlyList<ShelterConfig> shelters =
                [
                    new ShelterConfig("khs", "KHS", 2, "https://kshumane.org/adoption/pet-details/?aid={0}&cid=2&tid=Dog", "At KHS Since:"),
                    new ShelterConfig("kcpp", "KC Pet Project", 11, "https://kcpetproject.org/adopt/animal-details/?aid={0}&cid=11&tid=Dog", "Here Since:"),
                    new ShelterConfig("gpspca", "Great Plains SPCA", 17, "https://www.greatplainsspca.org/adopt/adoptable-animal-details/?aid={0}&cid=17&tid=Dog", "At GPSPCA Since:")
                ];
                services.AddSingleton(shelters);

                services.AddScoped<StateRepository>();
                services.AddScoped<DogRepository>();
                services.AddScoped<AdoptedDogRepository>();
                services.AddScoped<SubscriptionRepository>();

                services.AddScoped<ScrapingEngine>();
                services.AddScoped<DogDiffEngine>();
                services.AddScoped<NotificationEngine>();

                services.AddScoped<MonitorOrchestrator>();
                services.AddScoped<StatusOrchestrator>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        try
        {
            var tables = host.Services.GetRequiredService<TableServiceClient>();
            await tables.CreateTableIfNotExistsAsync("Dogs");
            await tables.CreateTableIfNotExistsAsync("SiteState");
            await tables.CreateTableIfNotExistsAsync("PushSubscriptions");
            await tables.CreateTableIfNotExistsAsync("AdoptedDogs");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Table pre-creation failed; tables will be created on first use");
        }

        await host.RunAsync();
    }
}
