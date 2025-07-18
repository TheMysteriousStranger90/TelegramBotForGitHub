namespace TelegramBotForGitHub.Commands.Core;

public interface ICommandFactory
{
    IEnumerable<ICommand> CreateCommands();
}