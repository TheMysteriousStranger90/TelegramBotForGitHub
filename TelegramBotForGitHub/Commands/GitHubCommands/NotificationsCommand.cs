using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class NotificationsCommand : TextBasedCommand
{
    protected override string Pattern => "notifications";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationsCommand> _logger;

    public NotificationsCommand(ITelegramBotClient telegramClient, IGitHubAuthService authService,
        HttpClient httpClient, ILogger<NotificationsCommand> logger)
    {
        _telegramClient = telegramClient;
        _authService = authService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        var userId = message.From!.Id;

        var isAuthorized = await _authService.IsUserAuthorizedAsync(userId);
        if (!isAuthorized)
        {
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "🔐 You need to authorize with GitHub first!\n\n" +
                      "Use `/auth` to connect your GitHub account.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
            return;
        }

        var userToken = await _authService.GetUserTokenAsync(userId);
        if (userToken == null)
        {
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Unable to retrieve your GitHub token.\n\n" +
                      "Please try `/logout` and then `/auth` to re-authorize.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
            return;
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {userToken.AccessToken}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var response =
                await _httpClient.GetAsync("https://api.github.com/notifications?per_page=10&participating=false");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await _telegramClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "🔐 Your GitHub token has expired.\n\n" +
                              "Please use `/logout` and then `/auth` to re-authorize.",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: CancellationToken.None);
                    return;
                }

                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: $"❌ Failed to get notifications.\n\n" +
                          $"Status: {response.StatusCode}\n" +
                          $"Please try again later.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var notifications = JsonSerializer.Deserialize<JsonElement[]>(content);

            if (notifications == null || notifications.Length == 0)
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "✅ **No new notifications!**\n\n" +
                          "You're all caught up! 🎉\n\n" +
                          "💡 **Tip:** Subscribe to repositories with `/subscribe owner/repo` to get notifications when something happens.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var notificationMessage = "🔔 **Recent Notifications:**\n\n";
            foreach (var notification in notifications.Take(5))
            {
                var title = notification.GetProperty("subject").GetProperty("title").GetString();
                var repo = notification.GetProperty("repository").GetProperty("full_name").GetString();
                var type = notification.GetProperty("subject").GetProperty("type").GetString();
                var reason = notification.GetProperty("reason").GetString();
                var unread = notification.GetProperty("unread").GetBoolean();
                var updatedAt = notification.GetProperty("updated_at").GetString();

                var lastUpdated = DateTime.TryParse(updatedAt, out var updateDate)
                    ? updateDate.ToString("MMM dd, HH:mm")
                    : "Unknown";

                var typeEmoji = type switch
                {
                    "Issue" => "🐛",
                    "PullRequest" => "🔀",
                    "Release" => "🚀",
                    "Discussion" => "💬",
                    _ => "📄"
                };

                var reasonEmoji = reason switch
                {
                    "author" => "✍️",
                    "assign" => "👤",
                    "comment" => "💬",
                    "mention" => "🏷️",
                    "review_requested" => "👀",
                    "team_mention" => "👥",
                    "state_change" => "🔄",
                    _ => "📌"
                };

                notificationMessage += $"{(unread ? "🔴" : "⚪")} **{title}**\n" +
                                       $"  📂 {repo}\n" +
                                       $"  {typeEmoji} {type} {reasonEmoji} {reason}\n" +
                                       $"  📅 {lastUpdated}\n\n";
            }

            if (notifications.Length > 5)
            {
                notificationMessage += $"... and {notifications.Length - 5} more notifications\n\n";
            }

            notificationMessage += "💡 **Tip:** Use `/clearnotifications` to mark all as read.";

            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: notificationMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing GitHub notifications for user {UserId}", userId);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Error parsing GitHub response.\n\n" +
                      "Please try again later.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Unexpected error occurred.\n\n" +
                      "Please try again later or contact support.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
    }
}