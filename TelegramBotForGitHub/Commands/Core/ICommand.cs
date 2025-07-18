using Telegram.Bot.Types;

namespace TelegramBotForGitHub.Commands.Core;

public interface ICommand
{
    bool CanExecute(Message message);
    Task Execute(Message message);
}