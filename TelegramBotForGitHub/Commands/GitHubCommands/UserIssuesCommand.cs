using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class UserIssuesCommand : TextBasedCommand
{
    protected override string Pattern => "userissues";

    private readonly ITelegramBotClient _telegramClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserIssuesCommand> _logger;

    public UserIssuesCommand(ITelegramBotClient telegramClient, HttpClient httpClient, ILogger<UserIssuesCommand> logger)
    {
        _telegramClient = telegramClient;
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        var parts = message.Text?.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts?.Length != 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            await _telegramClient.SendMessage(chatId, "Usage: `/userissues <github-username>`\nExample: `/userissues torvalds`", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            return;
        }

        var username = parts[1].Trim();

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        try
        {
            var url = $"https://api.github.com/search/issues?q=is:issue+is:open+author:{username}&sort=updated&order=desc&per_page=50";
            _logger.LogInformation("Fetching public issues for user {Username}", username);

            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub API error {Status} fetching issues for {Username}.", resp.StatusCode, username);
                await _telegramClient.SendMessage(chatId, $"❌ Failed to fetch issues for `{username}`. Status: {resp.StatusCode}", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                return;
            }

            var searchResult = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var issues = searchResult.GetProperty("items").EnumerateArray().ToList();

            if (issues.Count == 0)
            {
                await _telegramClient.SendMessage(chatId, $"📂 User `{username}` has no open public issues.", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                return;
            }

            const int chunkSize = 5;
            var chunks = issues
                .Select((issue, idx) => new { issue, idx })
                .GroupBy(x => x.idx / chunkSize)
                .Select(g => g.Select(x => x.issue).ToArray())
                .ToList();

            for (int i = 0; i < chunks.Count; i++)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"🐛 **{username}'s Open Issues** (batch {i + 1}/{chunks.Count}, total {issues.Count}):\n");

                foreach (var issue in chunks[i])
                {
                    var title = issue.GetProperty("title").GetString();
                    var issueNumber = issue.GetProperty("number").GetInt32();
                    var repoUrl = issue.GetProperty("repository_url").GetString()!.Replace("api.github.com/repos", "github.com");
                    var repoName = new Uri(repoUrl).AbsolutePath.Trim('/');
                    
                    sb.AppendLine($"• *{title}*");
                    sb.AppendLine($"  [`{repoName}#{issueNumber}`]({issue.GetProperty("html_url")})\n");
                }

                await _telegramClient.SendMessage(chatId, sb.ToString(), parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserIssuesCommand unexpected error for user {Username}", username);
            await _telegramClient.SendMessage(chatId, "❌ An unexpected error occurred while fetching user issues.");
        }
    }
}