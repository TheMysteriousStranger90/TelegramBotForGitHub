using Telegram.Bot.Types;

namespace TelegramBotForGitHub.Commands.Core;

public class CommandHandler
{
    private readonly ICommandFactory _commandFactory;
    private IEnumerable<ICommand>? _commands;

    public CommandHandler(ICommandFactory commandFactory)
    {
        _commandFactory = commandFactory;
    }

    public async Task Execute(Message message)
    {
        if (!IsValidCommand(message.Text ?? string.Empty))
        {
            return;
        }

        _commands ??= _commandFactory.CreateCommands();

        foreach (var command in _commands.Where(command => command.CanExecute(message)))
        {
            await command.Execute(message);
            break;
        }
    }

    private static bool IsValidCommand(string command) => !string.IsNullOrEmpty(command) && command.StartsWith('/');
}