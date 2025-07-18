using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Configuration;
using TelegramBotForGitHub.Services;
using TelegramBotForGitHub.Services.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Configuration mapping corrected to match local.settings.json
        services.Configure<BotConfiguration>(options =>
        {
            options.TelegramBotToken = Environment.GetEnvironmentVariable("Telegram__Token") ?? "";
            options.GitHub.Token = Environment.GetEnvironmentVariable("GitHub__Token") ?? "";
            options.GitHub.WebhookSecret = Environment.GetEnvironmentVariable("GitHub__WebhookSecret") ?? "";
            options.GitHub.ClientId = Environment.GetEnvironmentVariable("GitHub__ClientId") ?? "";
            options.GitHub.ClientSecret = Environment.GetEnvironmentVariable("GitHub__ClientSecret") ?? "";
            options.GitHub.AppId = Environment.GetEnvironmentVariable("GitHub__AppId") ?? "";
            options.GitHub.PrivateKey = Environment.GetEnvironmentVariable("GitHub__PrivateKey") ?? "";
            options.CosmosDB.ConnectionString = Environment.GetEnvironmentVariable("Cosmos__ConnectionString") ?? "";
            options.CosmosDB.DatabaseName = Environment.GetEnvironmentVariable("Cosmos__Database") ?? "";
            options.CosmosDB.ContainerName = Environment.GetEnvironmentVariable("Cosmos__Container") ?? "";
        });

        // Cosmos DB Client
        services.AddSingleton<CosmosClient>(provider =>
        {
            var config = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;
            var cosmosClientOptions = new CosmosClientOptions
            {
                RequestTimeout = TimeSpan.FromMinutes(5),
                ConnectionMode = ConnectionMode.Direct,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };
            return new CosmosClient(config.CosmosDB.ConnectionString, cosmosClientOptions);
        });
        
        services.AddSingleton<ICommandFactory, CommandFactory>();
        services.AddScoped<CommandHandler>();

        // Services
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddScoped<IGitHubService, GitHubService>();
        services.AddScoped<ITelegramBotService, TelegramBotService>();
        
        // Background service for cleaning expired auth states
        services.AddHostedService<AuthStateCleanupService>();
    })
    .Build();

host.Run();