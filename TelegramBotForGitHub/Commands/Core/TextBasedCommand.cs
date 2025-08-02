using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotForGitHub.Commands.Core
{
    public abstract class TextBasedCommand : ICommand
    {
        protected abstract string Pattern { get; }

        public bool CanExecute(Message message)
        {
            if (string.IsNullOrEmpty(message.Text))
            {
                return false;
            }

            var pattern = "/" + Pattern.ToLower();
            var messageText = message.Text.ToLower();
            
            bool startsWithCommand = messageText.StartsWith(pattern + " ") || messageText == pattern;

            bool startsWithGroupCommand = messageText.StartsWith(pattern + "@");

            var canExecute = startsWithCommand || (message.Chat.Type != ChatType.Private && startsWithGroupCommand);
            
            return canExecute;
        }

        public abstract Task Execute(Message message);
    }
}