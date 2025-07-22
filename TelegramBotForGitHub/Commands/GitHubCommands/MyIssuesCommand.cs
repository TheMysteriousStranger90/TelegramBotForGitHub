using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class MyIssuesCommand : TextBasedCommand
{
    protected override string Pattern => "myissues";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MyIssuesCommand> _logger;

    public MyIssuesCommand(ITelegramBotClient telegramClient, IGitHubAuthService authService, HttpClient httpClient, ILogger<MyIssuesCommand> logger)
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
            await _telegramClient.SendMessage(chatId, "🔐 You need to authorize with GitHub first! Use `/auth`.");
            return;
        }

        var token = await _authService.GetUserTokenAsync(userId);
        if (token == null)
        {
            await _telegramClient.SendMessage(chatId, "❌ Unable to retrieve your GitHub token. Try `/logout` + `/auth`.");
            return;
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        try
        {
            var url = "https://api.github.com/issues?filter=assigned&state=open&sort=updated&per_page=50";
            _logger.LogInformation("Fetching assigned issues for user {UserId}", userId);

            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub API error {Status} fetching assigned issues.", resp.StatusCode);
                await _telegramClient.SendMessage(chatId, $"❌ Failed to fetch your issues. Status: {resp.StatusCode}");
                return;
            }

            var issues = JsonSerializer.Deserialize<JsonElement[]>(await resp.Content.ReadAsStringAsync()) ?? Array.Empty<JsonElement>();

            if (issues.Length == 0)
            {
                await _telegramClient.SendMessage(chatId, "🎉 No open issues assigned to you. Great job!");
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
                sb.AppendLine($"🐛 **Your Open Issues** (batch {i + 1}/{chunks.Count}, total {issues.Length}):\n");

                foreach (var issue in chunks[i])
                {
                    var title = issue.GetProperty("title").GetString();
                    var issueNumber = issue.GetProperty("number").GetInt32();
                    var repoUrl = issue.GetProperty("repository").GetProperty("html_url").GetString();
                    var repoName = new Uri(repoUrl).AbsolutePath.Trim('/');
                    var author = issue.GetProperty("user").GetProperty("login").GetString();
                    var updatedAt = DateTime.Parse(issue.GetProperty("updated_at").GetString()!).ToString("yyyy-MM-dd");

                    sb.AppendLine($"• *{title}*");
                    sb.AppendLine($"  [`{repoName}#{issueNumber}`]({issue.GetProperty("html_url")}) by `{author}`");
                    sb.AppendLine($"  Last updated: {updatedAt}\n");
                }

                await _telegramClient.SendMessage(chatId, sb.ToString(), parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MyIssuesCommand unexpected error for user {UserId}", userId);
            await _telegramClient.SendMessage(chatId, "❌ An unexpected error occurred while fetching your issues.");
        }
    }
}