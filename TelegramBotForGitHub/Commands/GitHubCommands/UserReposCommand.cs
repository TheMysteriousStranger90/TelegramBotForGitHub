using System.Net;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;

namespace TelegramBotForGitHub.Commands.GitHubCommands
{
    public class UserReposCommand : TextBasedCommand
    {
        protected override string Pattern => "userrepos";

        private readonly ITelegramBotClient _telegramClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserReposCommand> _logger;

        public UserReposCommand(
            ITelegramBotClient telegramClient,
            HttpClient httpClient,
            ILogger<UserReposCommand> logger)
        {
            _telegramClient  = telegramClient;
            _httpClient      = httpClient;
            _logger          = logger;
        }
        
        public override async Task Execute(Message message)
        {
            var chatId = message.Chat.Id;
            var parts  = message.Text?.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts != null && (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[1])))
            {
                await _telegramClient.SendMessage(chatId,
                    "Usage: `/userrepos <github-username>`\n\n" +
                    "Example: `/userrepos octocat`",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                return;
            }

            var username = parts?[1].Trim();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotForGitHub");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var allRepos = new List<JsonElement>();
            int page     = 1, perPage = 100;

            try
            {
                while (true)
                {
                    var url = $"https://api.github.com/users/{username}/repos?sort=updated&per_page={perPage}&page={page}";
                    _logger.LogInformation("Fetching public repos of {User}: {Url}", username, url);

                    var resp = await _httpClient.GetAsync(url);
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        await _telegramClient.SendMessage(chatId,
                            $"❌ User `{username}` not found.",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        return;
                    }

                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError("GitHub API error {Status} for {Url}", resp.StatusCode, url);
                        await _telegramClient.SendMessage(chatId,
                            $"❌ Failed to fetch `{username}`’s repos. Status: {resp.StatusCode}");
                        return;
                    }

                    var arr = JsonSerializer.Deserialize<JsonElement[]>(
                        await resp.Content.ReadAsStringAsync()
                    ) ?? Array.Empty<JsonElement>();

                    if (arr.Length == 0) 
                        break;

                    allRepos.AddRange(arr);
                    page++;
                }

                if (allRepos.Count == 0)
                {
                    await _telegramClient.SendMessage(chatId,
                        $"📂 User `{username}` has no public repositories.",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    return;
                }

                const int chunkSize = 10;
                var chunks = allRepos
                    .Select((r, i) => new { r, i })
                    .GroupBy(x => x.i / chunkSize)
                    .Select(g => g.Select(x => x.r).ToArray())
                    .ToList();

                for (int i = 0; i < chunks.Count; i++)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"📂 `{username}`’s repos (batch {i+1}/{chunks.Count}, total {allRepos.Count}):\n");

                    foreach (var repo in chunks[i])
                    {
                        var name        = repo.GetProperty("full_name").GetString();
                        var desc        = repo.TryGetProperty("description", out var d) && d.ValueKind != JsonValueKind.Null
                                          ? d.GetString() 
                                          : "—";
                        var lang        = repo.TryGetProperty("language", out var l) && l.ValueKind != JsonValueKind.Null
                                          ? l.GetString()
                                          : "Unknown";
                        var stars       = repo.GetProperty("stargazers_count").GetInt32();
                        var forks       = repo.GetProperty("forks_count").GetInt32();
                        var isPrivate   = repo.GetProperty("private").GetBoolean() ? "🔒" : "🌐";
                        var updatedAt   = repo.GetProperty("updated_at").GetString();
                        var updatedDate = DateTime.TryParse(updatedAt, out var dt)
                                          ? dt.ToString("yyyy-MM-dd")
                                          : "Unknown";

                        sb.AppendLine($"• *{name}* {isPrivate}");
                        sb.AppendLine($"  _{desc}_");
                        sb.AppendLine($"  `{lang}` ⭐{stars} 🍴{forks}  • {updatedDate}\n");
                    }

                    await _telegramClient.SendMessage(
                        chatId: chatId,
                        text: sb.ToString(),
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserReposCommand for user {UserId}", message.From!.Id);
                await _telegramClient.SendMessage(chatId,
                    "❌ Unexpected error occurred. Please try again later or contact support.");
            }
        }
    }
}
