using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class SubReposCommand : TextBasedCommand
{
    protected override string Pattern => "subrepos";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IDbService _dbService;
    private readonly ILogger<SubReposCommand> _logger;

    public SubReposCommand(ITelegramBotClient telegramClient, IDbService cosmosDbService, ILogger<SubReposCommand> logger)
    {
        _telegramClient = telegramClient;
        _dbService = cosmosDbService;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        try
        {
            var subscriptions = await _dbService.GetChatSubscriptionsAsync(message.Chat.Id);
            var activeSubscriptions = subscriptions.Where(s => s.IsActive).ToList();
            
            if (!activeSubscriptions.Any())
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "📭 **No active subscriptions**\n\n" +
                          "You haven't subscribed to any repositories yet.\n\n" +
                          "**Get started:**\n" +
                          "• Use `/subscribe owner/repo` to subscribe to a repository\n" +
                          "• Use `/myrepos` to see your GitHub repositories\n" +
                          "• Use `/help` for more commands",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var message_text = "📋 **Your Repository Subscriptions:**\n\n";
            
            foreach (var subscription in activeSubscriptions.OrderBy(s => s.RepositoryUrl))
            {
                var repoName = subscription.RepositoryUrl.Replace("https://github.com/", "");
                var eventsText = string.Join(", ", subscription.Events.Select(e => e switch
                {
                    "push" => "🔄 Push",
                    "pull_request" => "🔀 Pull Request",
                    "issues" => "🐛 Issues",
                    _ => e
                }));
                
                var createdDate = subscription.CreatedAt.ToString("yyyy-MM-dd");
                
                message_text += $"**{repoName}**\n" +
                               $"  Events: {eventsText}\n" +
                               $"  Since: {createdDate}\n\n";
            }

            message_text += $"**Total:** {activeSubscriptions.Count} active subscription(s)\n\n";
            message_text += "💡 **Tips:**\n";
            message_text += "• Use `/unsubscribe owner/repo` to remove a subscription\n";
            message_text += "• Use `/subscribe owner/repo events` to modify events\n";
            message_text += "• Configure GitHub webhooks for real-time notifications";

            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: message_text,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscriptions for chat {ChatId}", message.Chat.Id);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Failed to get your subscriptions.\n\n" +
                      "Please try again later.",
                cancellationToken: CancellationToken.None);
        }
    }
}