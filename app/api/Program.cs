using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Core.DomainModels;
using Core.Engines;
using Core.Repositories;
using Api.Orchestrators;

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
                services.AddMemoryCache();
                services.AddHttpClient("PetBridge", c => c.Timeout = TimeSpan.FromSeconds(30));

                IReadOnlyList<ShelterConfig> shelters =
                [
                    new ShelterConfig("khs", "KHS", 2, "https://kshumane.org/adoption/pet-details/?aid={0}&cid=2&tid=Dog", "At KHS Since:"),
                    new ShelterConfig("kcpp", "KC Pet Project", 11, "https://kcpetproject.org/adopt/animal-details/?aid={0}&cid=11&tid=Dog", "Here Since:"),
                    new ShelterConfig("gpspca", "Great Plains SPCA", 17, "https://www.greatplainsspca.org/adopt/adoptable-animal-details/?aid={0}&cid=17&tid=Dog", "At GPSPCA Since:")
                ];
                services.AddSingleton(shelters);

                IReadOnlyList<ShelterLuvConfig> shelterLuvShelters =
                [
                    new ShelterLuvConfig("ptdr", "Pawsitive Tails Dog Rescue", "https://www.pawsitivetailskc.org/wp-admin/admin-ajax.php?action=getAllDogs", "https://www.pawsitivetailskc.org/adopt/dog/?id={0}")
                ];
                services.AddSingleton(shelterLuvShelters);

                IReadOnlyList<ShelterLuvV3Config> shelterLuvV3Shelters =
                [
                    new ShelterLuvV3Config("hsgkc", "Humane Society of Greater Kansas City", "https://www.shelterluv.com/api/v3/available-animals/26960?type=Dog")
                ];
                services.AddSingleton(shelterLuvV3Shelters);

                services.AddScoped<StateRepository>();
                services.AddScoped<DogRepository>();
                services.AddScoped<AdoptedDogRepository>();
                services.AddScoped<SubscriptionRepository>();
                services.AddScoped<FavoritesRepository>();

                services.AddScoped<ScrapingEngine>();
                services.AddScoped<DogDiffEngine>();
                services.AddScoped<NotificationEngine>();

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
            await tables.CreateTableIfNotExistsAsync("Favorites");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Table pre-creation failed; tables will be created on first use");
        }

        await host.RunAsync();
    }
}
