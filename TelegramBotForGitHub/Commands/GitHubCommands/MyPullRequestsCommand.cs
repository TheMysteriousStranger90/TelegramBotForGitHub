using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class MyPullRequestsCommand : TextBasedCommand
{
    protected override string Pattern => "mypullrequests";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MyPullRequestsCommand> _logger;

    public MyPullRequestsCommand(ITelegramBotClient telegramClient, IGitHubAuthService authService, HttpClient httpClient, ILogger<MyPullRequestsCommand> logger)
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
            var url = "https://api.github.com/search/issues?q=is:pr+is:open+involves:@me&sort=updated&order=desc&per_page=50";
            _logger.LogInformation("Fetching involved PRs for user {UserId}", userId);

            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("GitHub API error {Status} fetching involved PRs.", resp.StatusCode);
                await _telegramClient.SendMessage(chatId, $"❌ Failed to fetch your pull requests. Status: {resp.StatusCode}");
                return;
            }

            var searchResult = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
            var prs = searchResult.GetProperty("items").EnumerateArray().ToList();

            if (prs.Count == 0)
            {
                await _telegramClient.SendMessage(chatId, "🎉 No open pull requests involving you. Looks clean!");
                return;
            }

            const int chunkSize = 5;
            var chunks = prs
                .Select((pr, idx) => new { pr, idx })
                .GroupBy(x => x.idx / chunkSize)
                .Select(g => g.Select(x => x.pr).ToArray())
                .ToList();

            for (int i = 0; i < chunks.Count; i++)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"🔀 **Your Open Pull Requests** (batch {i + 1}/{chunks.Count}, total {prs.Count}):\n");

                foreach (var pr in chunks[i])
                {
                    var title = pr.GetProperty("title").GetString();
                    var prNumber = pr.GetProperty("number").GetInt32();
                    var repoUrl = pr.GetProperty("repository_url").GetString()!.Replace("api.github.com/repos", "github.com");
                    var repoName = new Uri(repoUrl).AbsolutePath.Trim('/');
                    var author = pr.GetProperty("user").GetProperty("login").GetString();
                    
                    sb.AppendLine($"• *{title}*");
                    sb.AppendLine($"  [`{repoName}#{prNumber}`]({pr.GetProperty("html_url")}) by `{author}`\n");
                }

                await _telegramClient.SendMessage(chatId, sb.ToString(), parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MyPullRequestsCommand unexpected error for user {UserId}", userId);
            await _telegramClient.SendMessage(chatId, "❌ An unexpected error occurred while fetching your pull requests.");
        }
    }
}