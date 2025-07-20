using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Telegram.Bot;
using TelegramBotForGitHub.Services;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Commands.GitHubCommands;
using TelegramBotForGitHub.Converters;
using TelegramBotForGitHub.Models.Configuration;
using TelegramBotForGitHub.Services.Interfaces;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults((WorkerOptions workerOptions) =>
    {
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Register configuration classes
        services.Configure<GitHubConfiguration>(configuration.GetSection("GitHub"));
        services.Configure<TelegramConfiguration>(configuration.GetSection("Telegram"));

        // Telegram Bot
        var telegramToken = configuration["Telegram:Token"];
        if (!string.IsNullOrEmpty(telegramToken))
        {
            services.AddSingleton<ITelegramBotClient>(provider =>
                new TelegramBotClient(telegramToken));
        }
        
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = { 
                new UnixDateTimeConverter(),
                new TelegramEnumConverter()
            },
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Error = (sender, args) =>
            {
                args.ErrorContext.Handled = true;
            }
        };

        // Services - Replace CosmosDB with Table Storage
        services.AddScoped<TableStorageService>();
        services.AddScoped<IDbService>(serviceProvider =>
        {
            var innerService = serviceProvider.GetRequiredService<TableStorageService>();
            return innerService;
        });
        
        services.AddScoped<ITelegramService, TelegramService>();
        services.AddScoped<IGitHubAuthService, GitHubAuthService>();

        // Command Handler
        services.AddScoped<CommandHandler>();

        // Register commands in specific order - UnhandledCommand must be last
        services.AddScoped<ICommand, StartCommand>();
        services.AddScoped<ICommand, AuthCommand>();
        services.AddScoped<ICommand, ProfileCommand>();
        services.AddScoped<ICommand, HelpCommand>();
        services.AddScoped<ICommand, LogoutCommand>();
        services.AddScoped<ICommand, MyReposCommand>();
        services.AddScoped<ICommand, NotificationsCommand>();
        services.AddScoped<ICommand, ClearNotificationsCommand>();
        services.AddScoped<ICommand, SubscribeCommand>();
        services.AddScoped<ICommand, UnsubscribeCommand>();
        services.AddScoped<ICommand, ReposCommand>();
        services.AddScoped<ICommand, StatusCommand>();
        services.AddScoped<ICommand, PingCommand>();

        // UnhandledCommand should be registered last
        services.AddScoped<ICommand, UnhandledCommand>();

        // HTTP Client
        services.AddHttpClient();
        
    })
    .Build();

host.Run();