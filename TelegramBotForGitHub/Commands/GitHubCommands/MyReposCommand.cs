using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class MyReposCommand : TextBasedCommand
{
    protected override string Pattern => "myrepos";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<MyReposCommand> _logger;

    public MyReposCommand(ITelegramBotClient telegramClient, IGitHubAuthService authService, HttpClient httpClient,
        ILogger<MyReposCommand> logger)
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
            await _telegramClient.SendMessage(chatId,
                "❌ Unable to retrieve your GitHub token. Try `/logout` + `/auth`.");
            return;
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {token.AccessToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

        var allRepos = new List<JsonElement>();
        int page = 1, perPage = 100;

        try
        {
            while (true)
            {
                var url = $"https://api.github.com/user/repos?sort=updated&type=all&per_page={perPage}&page={page}";
                _logger.LogInformation("Fetch page {Page}: {Url}", page, url);

                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("GitHub API {Status} at {Url}", resp.StatusCode, url);
                    await _telegramClient.SendMessage(chatId,
                        $"❌ Failed to fetch repos (page {page}). Status: {resp.StatusCode}");
                    return;
                }

                var pageRepos = JsonSerializer
                                    .Deserialize<JsonElement[]>(await resp.Content.ReadAsStringAsync())
                                ?? Array.Empty<JsonElement>();

                if (pageRepos.Length == 0) break;

                allRepos.AddRange(pageRepos);
                page++;
            }

            if (allRepos.Count == 0)
            {
                await _telegramClient.SendMessage(chatId, "📂 No repositories found.");
                return;
            }

            const int chunkSize = 10;
            var chunks = allRepos
                .Select((repo, idx) => new { repo, idx })
                .GroupBy(x => x.idx / chunkSize)
                .Select(g => g.Select(x => x.repo).ToArray())
                .ToList();

            for (int i = 0; i < chunks.Count; i++)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"📂 Repos (batch {i + 1}/{chunks.Count}, total {allRepos.Count}):\n");

                foreach (var repo in chunks[i])
                {
                    var name = repo.GetProperty("full_name").GetString();
                    var desc = repo.TryGetProperty("description", out var d) && d.ValueKind != JsonValueKind.Null
                        ? d.GetString()
                        : "No description";
                    var lang = repo.TryGetProperty("language", out var l) && l.ValueKind != JsonValueKind.Null
                        ? l.GetString()
                        : "Unknown";
                    var stars = repo.GetProperty("stargazers_count").GetInt32();
                    var forks = repo.GetProperty("forks_count").GetInt32();
                    var priv = repo.GetProperty("private").GetBoolean() ? "🔒" : "🌐";
                    var updated = repo.GetProperty("updated_at").GetString();
                    var date = DateTime.TryParse(updated, out var dt) ? dt.ToString("yyyy-MM-dd") : "Unknown";

                    sb.AppendLine($"• {name} {priv}");
                    sb.AppendLine($"  {desc}");
                    sb.AppendLine($"  {lang} ⭐{stars} 🍴{forks}  {date}\n");
                }

                try
                {
                    await _telegramClient.SendMessage(chatId, sb.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send batch {Batch}", i + 1);
                    await _telegramClient.SendMessage(chatId, $"⚠️ Failed to send batch {i + 1}. See logs.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MyReposCommand unexpected error for user {UserId}", userId);
            await _telegramClient.SendMessage(chatId,
                "❌ Unexpected error occurred. Please try again later or contact support.");
        }
    }
}