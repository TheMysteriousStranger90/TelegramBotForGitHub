using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace TelegramBotForGitHub.Commands.Core
{
    public class CommandHandler
    {
        private readonly IEnumerable<ICommand> _commands;
        private readonly ILogger<CommandHandler> _logger;

        public CommandHandler(IEnumerable<ICommand> commands, ILogger<CommandHandler> logger)
        {
            _commands = commands;
            _logger = logger;
        }

        public async Task Execute(Message message)
        {
            if (message?.Text == null)
            {
                _logger.LogInformation("CommandHandler: Received a message without text. Skipping command execution.");
                return;
            }

            var messageText = message.Text;
            var userId = message.From?.Id;

            _logger.LogInformation("CommandHandler: Processing message '{MessageText}' from user {UserId}",
                messageText, userId);

            try
            {
                _logger.LogInformation("CommandHandler: Available commands: {Commands}", 
                    string.Join(", ", _commands.Select(c => c.GetType().Name)));

                var matchingCommands = _commands.Where(c => c.CanExecute(message)).ToList();
                _logger.LogInformation("CommandHandler: Found {Count} matching commands: {Commands}", 
                    matchingCommands.Count, 
                    string.Join(", ", matchingCommands.Select(c => c.GetType().Name)));

                var commandToExecute = matchingCommands.FirstOrDefault();

                if (commandToExecute != null)
                {
                    _logger.LogInformation("CommandHandler: Executing command {CommandType} for user {UserId}",
                        commandToExecute.GetType().Name, userId);

                    await commandToExecute.Execute(message);

                    _logger.LogInformation("CommandHandler: Command {CommandType} executed successfully for user {UserId}",
                        commandToExecute.GetType().Name, userId);
                }
                else
                {
                    _logger.LogWarning("CommandHandler: No command found for message '{MessageText}' from user {UserId}. " +
                                       "Please ensure commands are properly registered.",
                        messageText, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommandHandler: Error executing command for message '{MessageText}' from user {UserId}",
                    messageText, userId);
                throw;
            }
        }
    }
}