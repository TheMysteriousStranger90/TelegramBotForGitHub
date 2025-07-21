using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        // Services
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

        // Register commands
        services.AddScoped<ICommand, StartCommand>();
        services.AddScoped<ICommand, AuthCommand>();
        services.AddScoped<ICommand, ProfileCommand>();
        services.AddScoped<ICommand, HelpCommand>();
        services.AddScoped<ICommand, LogoutCommand>();
        services.AddScoped<ICommand, MyReposCommand>();
        services.AddScoped<ICommand, UserReposCommand>();
        services.AddScoped<ICommand, MyIssuesCommand>();
        services.AddScoped<ICommand, UserIssuesCommand>();
        services.AddScoped<ICommand, MyPullRequestsCommand>();
        services.AddScoped<ICommand, UserPullRequestsCommand>();
        services.AddScoped<ICommand, SubReposCommand>();
        services.AddScoped<ICommand, SubscribeCommand>();
        services.AddScoped<ICommand, UnsubscribeCommand>();
        services.AddScoped<ICommand, SubNotificationsCommand>();
        services.AddScoped<ICommand, MyNotificationsCommand>();
        services.AddScoped<ICommand, UserNotificationsCommand>();
        
        services.AddScoped<ICommand, UnhandledCommand>();

        // HTTP Client
        services.AddHttpClient();
        
    })
    .Build();

host.Run();