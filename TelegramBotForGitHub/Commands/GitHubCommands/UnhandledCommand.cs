using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;

namespace TelegramBotForGitHub.Commands.GitHubCommands
{
    public class UnhandledCommand : ICommand
    {
        private readonly ITelegramBotClient _telegramClient;
        private readonly ILogger<UnhandledCommand> _logger;

        public UnhandledCommand(ITelegramBotClient telegramClient, ILogger<UnhandledCommand> logger)
        {
            _telegramClient = telegramClient;
            _logger = logger;
        }

        public bool CanExecute(Message message)
        {
            // Only handle messages that start with "/" but don't match any known commands
            if (string.IsNullOrEmpty(message.Text) || !message.Text.StartsWith("/"))
            {
                return false;
            }

            // Extract command name (remove bot username if present)
            var commandText = message.Text.Split(' ')[0].Split('@')[0].ToLower();
            
            // List of known commands - this should match your registered commands
            var knownCommands = new[]
            {
                "/start", "/help", "/auth", "/profile", "/myrepos", 
                "/subscribe", "/unsubscribe", "/notifications", "/status", "/logout"
            };

            var isKnownCommand = knownCommands.Contains(commandText);
            
            _logger.LogInformation("UnhandledCommand: Checking command '{Command}', isKnown: {IsKnown}", 
                commandText, isKnownCommand);
            
            return !isKnownCommand;
        }

        public async Task Execute(Message message)
        {
            _logger.LogInformation("UnhandledCommand: Executing for message '{Text}'", message.Text);
            
            var displayString = $"❓ **Unknown command**\n\n" +
                                $"I don't recognize the command `{message.Text?.Split(' ')[0]}`.\n\n" +
                                $"**Available commands:**\n" +
                                $"• `/help` - Show all available commands\n" +
                                $"• `/start` - Show welcome message\n" +
                                $"• `/auth` - Authorize with GitHub\n" +
                                $"• `/profile` - Show GitHub profile\n" +
                                $"• `/myrepos` - List your repositories\n" +
                                $"• `/subscribe` - Subscribe to repository notifications\n" +
                                $"• `/status` - Show bot status\n\n" +
                                $"Use `/help` for a complete list of commands.";
            
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: displayString,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
    }
}