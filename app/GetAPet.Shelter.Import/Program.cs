using Azure.Data.Tables;
using Core.DomainModels;
using Core.Engines;
using Core.Orchestrators;
using Core.Repositories;

namespace GetAPet.Shelter.Import
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.AddUserSecrets<Program>(optional: true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    var connectionString = ctx.Configuration["STORAGE_CONNECTION_STRING"];
                    services.AddSingleton(_ => new TableServiceClient(connectionString));
                    services.AddMemoryCache();
                    services.AddHttpClient("PetBridge", c => c.Timeout = TimeSpan.FromSeconds(30));

                    IReadOnlyList<ShelterConfig> shelters = new List<ShelterConfig>
                    {
                        new ShelterConfig("khs", "KHS", 2, "https://kshumane.org/adoption/pet-details/?aid={0}&cid=2&tid=Dog", "At KHS Since:"),
                        new ShelterConfig("kcpp", "KC Pet Project", 11, "https://kcpetproject.org/adopt/animal-details/?aid={0}&cid=11&tid=Dog", "Here Since:"),
                        new ShelterConfig("gpspca", "Great Plains SPCA", 17, "https://www.greatplainsspca.org/adopt/adoptable-animal-details/?aid={0}&cid=17&tid=Dog", "At GPSPCA Since:")
                    };
                    services.AddSingleton(shelters);

                    services.AddScoped<StateRepository>();
                    services.AddScoped<DogRepository>();
                    services.AddScoped<AdoptedDogRepository>();
                    services.AddScoped<SubscriptionRepository>();

                    services.AddScoped<ScrapingEngine>();
                    services.AddScoped<DogDiffEngine>();
                    services.AddScoped<NotificationEngine>();

                    services.AddScoped<MonitorOrchestrator>();

                    services.AddHostedService<MonitorWorker>();
                })
                .Build();


            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            try
            {
                logger.LogInformation("Validating configuration values...");

                ArgumentException.ThrowIfNullOrWhiteSpace(configuration["STORAGE_CONNECTION_STRING"], "STORAGE_CONNECTION_STRING");
                ArgumentException.ThrowIfNullOrWhiteSpace(configuration["VAPID_SUBJECT"], "VAPID_SUBJECT");
                ArgumentException.ThrowIfNullOrWhiteSpace(configuration["VAPID_PUBLIC_KEY"], "VAPID_PUBLIC_KEY");
                ArgumentException.ThrowIfNullOrWhiteSpace(configuration["VAPID_PRIVATE_KEY"], "VAPID_PRIVATE_KEY");

                logger.LogInformation("All required configuration values found");

                var tables = host.Services.GetRequiredService<TableServiceClient>();
                
                await tables.CreateTableIfNotExistsAsync("Dogs");
                await tables.CreateTableIfNotExistsAsync("SiteState");
                await tables.CreateTableIfNotExistsAsync("PushSubscriptions");
                await tables.CreateTableIfNotExistsAsync("AdoptedDogs");
            }
            catch (ArgumentException ex)
            {
                logger.LogCritical(ex, $"Missing required configuration value: <{ex.ParamName}>");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Table pre-creation failed; tables will be created on first use");
            }

            await host.RunAsync();
        }
    }
}