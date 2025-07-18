using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotForGitHub.Commands.Core;

public abstract class TextBasedCommand : ICommand
{
    protected abstract string Pattern { get; }

    public bool CanExecute(Message message)
    {
        var pattern = "/" + Pattern;
        
        return message.Text == pattern
               || message.Chat.Type == ChatType.Group && message.Text?.StartsWith(pattern + "@") == true;
    }

    public abstract Task Execute(Message message);
}