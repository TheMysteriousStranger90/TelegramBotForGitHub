using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotForGitHub.Commands.Core;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class UserNotificationsCommand : TextBasedCommand
{
    protected override string Pattern => "usernotifications";

    private readonly ITelegramBotClient _telegramClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserNotificationsCommand> _logger;

    public UserNotificationsCommand(
        ITelegramBotClient telegramClient,
        HttpClient httpClient,
        ILogger<UserNotificationsCommand> logger)
    {
        _telegramClient = telegramClient;
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        var parts = message.Text?.Trim()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts?.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            await _telegramClient.SendMessage(chatId,
                "Usage: `/usernotifications <github-username>`\n" +
                "Example: `/usernotifications octocat`",
                parseMode: ParseMode.Markdown);
            return;
        }

        var username = parts[1].Trim();

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        try
        {
            _logger.LogInformation("Fetching mentions for user {Username}", username);
            var url = $"https://api.github.com/search/issues?" +
                      $"q=mentions:{username}+state:open&sort=updated&order=desc&per_page=50";
            var resp = await _httpClient.GetAsync(url);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub API error {Status} fetching mentions for {Username}.",
                    resp.StatusCode, username);
                await _telegramClient.SendMessage(chatId,
                    $"❌ Failed to fetch notifications for `{username}`. Status: {resp.StatusCode}",
                    parseMode: ParseMode.Markdown);
                return;
            }

            var root = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var items = root.GetProperty("items").EnumerateArray().ToList();

            if (items.Count == 0)
            {
                await _telegramClient.SendMessage(chatId,
                    $"📂 User `{username}` has no open mentions.",
                    parseMode: ParseMode.Markdown);
                return;
            }

            const int chunkSize = 5;
            var batches = items
                .Select((it, idx) => new { it, idx })
                .GroupBy(x => x.idx / chunkSize)
                .Select(g => g.Select(x => x.it).ToArray())
                .ToList();

            for (int i = 0; i < batches.Count; i++)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"🔔 **{username}'s Mentions** (batch {i + 1}/{batches.Count}, total {items.Count}):\n");

                foreach (var it in batches[i])
                {
                    var title = it.GetProperty("title").GetString();
                    var number = it.GetProperty("number").GetInt32();
                    var repoUrl = it.GetProperty("repository_url").GetString()!
                        .Replace("api.github.com/repos", "github.com");
                    var repoName = new Uri(repoUrl).AbsolutePath.Trim('/');
                    var htmlUrl = it.GetProperty("html_url").GetString();

                    sb.AppendLine($"• *{title}*");
                    sb.AppendLine($"  [`{repoName}#{number}`]({htmlUrl})");
                    sb.AppendLine();
                }

                await _telegramClient.SendMessage(
                    chatId,
                    sb.ToString(),
                    parseMode: ParseMode.Markdown);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserNotificationsCommand unexpected error for user {Username}", username);
            await _telegramClient.SendMessage(chatId,
                "❌ An unexpected error occurred while fetching user notifications.",
                parseMode: ParseMode.Markdown);
        }
    }
}