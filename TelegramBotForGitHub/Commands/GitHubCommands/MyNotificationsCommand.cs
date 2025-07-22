using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class MyNotificationsCommand : TextBasedCommand
{
    protected override string Pattern => "mynotifications";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MyNotificationsCommand> _logger;

    public MyNotificationsCommand(
        ITelegramBotClient telegramClient,
        IGitHubAuthService authService,
        HttpClient httpClient,
        ILogger<MyNotificationsCommand> logger)
    {
        _telegramClient = telegramClient;
        _authService = authService;
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;

        if (!await _authService.IsUserAuthorizedAsync(userId))
        {
            await _telegramClient.SendMessage(chatId,
                "🔐 You need to authorize with GitHub first! Use `/auth`.",
                parseMode: ParseMode.Markdown);
            return;
        }

        var token = await _authService.GetUserTokenAsync(userId);
        if (token == null)
        {
            await _telegramClient.SendMessage(chatId,
                "❌ Unable to retrieve your GitHub token. Try `/logout` + `/auth`.",
                parseMode: ParseMode.Markdown);
            return;
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        try
        {
            _logger.LogInformation("Fetching notifications for user {UserId}", userId);
            var url = "https://api.github.com/notifications?per_page=50";
            var resp = await _httpClient.GetAsync(url);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub API error {Status} fetching notifications.", resp.StatusCode);
                await _telegramClient.SendMessage(chatId,
                    $"❌ Failed to fetch your notifications. Status: {resp.StatusCode}",
                    parseMode: ParseMode.Markdown);
                return;
            }

            var notifications = JsonSerializer
                                    .Deserialize<JsonElement[]>(await resp.Content.ReadAsStringAsync())
                                ?? Array.Empty<JsonElement>();

            if (notifications.Length == 0)
            {
                await _telegramClient.SendMessage(chatId,
                    "🎉 No unread notifications! You’re all caught up.",
                    parseMode: ParseMode.Markdown);
                return;
            }

            const int chunkSize = 5;
            var batches = notifications
                .Select((n, i) => new { n, i })
                .GroupBy(x => x.i / chunkSize)
                .Select(g => g.Select(x => x.n).ToArray())
                .ToList();

            for (int i = 0; i < batches.Count; i++)
            {
                var sb = new StringBuilder();
                sb.AppendLine(
                    $"🔔 **Your GitHub Notifications** (batch {i + 1}/{batches.Count}, total {notifications.Length}):\n");

                foreach (var note in batches[i])
                {
                    var repo = note.GetProperty("repository").GetProperty("full_name").GetString();
                    var subj = note.GetProperty("subject");
                    var title = subj.GetProperty("title").GetString();
                    var type = subj.GetProperty("type").GetString();
                    var reason = note.GetProperty("reason").GetString();
                    var updatedAt = DateTime.Parse(note.GetProperty("updated_at").GetString()!)
                        .ToString("yyyy-MM-dd");
                    var apiUrl = subj.GetProperty("url").GetString();
                    var webUrl = apiUrl?
                        .Replace("api.github.com/repos", "github.com")
                        .Replace("/pulls/", "/pull/")
                        .Replace("/issues/", "/issues/") ?? "";

                    sb.AppendLine($"• *[{repo}] {title}*");
                    sb.AppendLine($"  Type: `{type}` • Reason: `{reason}`");
                    if (!string.IsNullOrEmpty(webUrl))
                        sb.AppendLine($"  [View on GitHub]({webUrl})");
                    sb.AppendLine($"  Updated: {updatedAt}\n");
                }

                await _telegramClient.SendMessage(
                    chatId,
                    sb.ToString(),
                    parseMode: ParseMode.Markdown);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MyNotificationsCommand unexpected error for user {UserId}", userId);
            await _telegramClient.SendMessage(chatId,
                "❌ An unexpected error occurred while fetching your notifications.",
                parseMode: ParseMode.Markdown);
        }
    }
}