using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace TelegramBotForGitHub.Commands.GitHubCommands
{
    public class SubNotificationsCommand : TextBasedCommand
    {
        protected override string Pattern => "subnotifications";

        private readonly ITelegramBotClient _telegramClient;
        private readonly IDbService _dbService;
        private readonly ILogger<SubNotificationsCommand> _logger;

        public SubNotificationsCommand(
            ITelegramBotClient telegramClient,
            IDbService dbService,
            ILogger<SubNotificationsCommand> logger)
        {
            _telegramClient = telegramClient;
            _dbService = dbService;
            _logger = logger;
        }

        public override async Task Execute(Message message)
        {
            try
            {
                var chatId = message.Chat.Id;
                var logs = await _dbService.GetNotificationLogsAsync(chatId);

                if (logs == null || !logs.Any())
                {
                    await _telegramClient.SendMessage(
                        chatId: chatId,
                        text: "📭 **No recent notifications**\n\n" +
                              "You haven't received any GitHub notifications yet.",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: CancellationToken.None);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("📰 **Recent GitHub Notifications:**\n");

                foreach (var entry in logs)
                {
                    var timeLocal = entry.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                    var repoName = entry.RepositoryUrl?
                                       .Replace("https://github.com/", "") 
                                   ?? "unknown/unknown";
                    var evtType = entry.EventType ?? "event";
                    sb.AppendLine($"• {timeLocal} — **{repoName}** — {evtType}");
                }

                await _telegramClient.SendMessage(
                    chatId: chatId,
                    text: sb.ToString(),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications for chat {ChatId}", message.Chat.Id);
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "❌ Failed to retrieve your notification history. Please try again later.",
                    cancellationToken: CancellationToken.None);
            }
        }
    }
}
