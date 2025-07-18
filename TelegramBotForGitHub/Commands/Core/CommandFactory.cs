using Microsoft.Extensions.DependencyInjection;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.Core;

public class CommandFactory : ICommandFactory
{
    private readonly IServiceProvider _serviceProvider;

    public CommandFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<ICommand> CreateCommands()
    {
        using var scope = _serviceProvider.CreateScope();
        
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();
        var userSessionService = scope.ServiceProvider.GetRequiredService<IUserSessionService>();
        var gitHubService = scope.ServiceProvider.GetRequiredService<IGitHubService>();
        
        yield return new StartCommand(telegramService, userSessionService);
        yield return new AuthCommand(telegramService, userSessionService, gitHubService);
        yield return new HelpCommand(telegramService);
        yield return new ProfileCommand(telegramService, userSessionService, gitHubService);
        yield return new ReposCommand(telegramService, userSessionService, gitHubService);
        yield return new LogoutCommand(telegramService, userSessionService);
        
        yield return new UnhandledCommand(telegramService);
    }
}