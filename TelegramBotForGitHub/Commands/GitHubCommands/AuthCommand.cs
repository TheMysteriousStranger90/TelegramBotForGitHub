using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands
{
    public class AuthCommand : TextBasedCommand
    {
        protected override string Pattern => "auth";

        private readonly ITelegramBotClient _bot;
        private readonly IGitHubAuthService _auth;
        private readonly ILogger<AuthCommand> _logger;

        public AuthCommand(
            ITelegramBotClient botClient,
            IGitHubAuthService gitHubAuthService,
            ILogger<AuthCommand> logger)
        {
            _bot    = botClient;
            _auth   = gitHubAuthService;
            _logger = logger;
        }

        public override async Task Execute(Message message)
        {
            var chatId = message.Chat.Id;
            var userId = message.From?.Id ?? 0;
            if (userId == 0)
            {
                await _bot.SendMessage(chatId, "❌ Cannot identify you.");
                return;
            }

            var processing = await _bot.SendMessage(
                chatId, "🔄 Checking your authorization...");

            bool isAuth;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                isAuth = await _auth.IsUserAuthorizedAsync(userId)
                                  .WaitAsync(cts.Token);
            }
            catch
            {
                isAuth = false;
            }

            if (isAuth)
            {
                _logger.LogInformation("User {UserId} already authorized", userId);
                await _bot.EditMessageText(
                    chatId, processing.MessageId,
                    "✅ You’re already authenticated!\nUse /profile, /myrepos, /subscribe, /notifications.",
                    parseMode: ParseMode.Markdown);
                return;
            }

            string url;
            try
            {
                url = await _auth.GetAuthorizationUrl(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build auth URL for {UserId}", userId);
                await _bot.EditMessageText(
                    chatId, processing.MessageId,
                    "❌ Cannot generate auth link, try later.");
                return;
            }

            var text = $"🔐 *GitHub Authorization Required*\n\n" +
                       $"[Authorize Here]({url})\n\n" +
                       $"_Link expires in 10 minutes._";

            await _bot.EditMessageText(
                chatId, processing.MessageId,
                text, parseMode: ParseMode.Markdown);

            _logger.LogInformation("Sent auth link to user {UserId}", userId);
        }
    }
}
