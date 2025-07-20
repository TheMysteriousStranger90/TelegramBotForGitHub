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
                await _httpClient.GetAsync("https://api.github.com/user/repos?sort=updated&per_page=10&type=all");

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
                    text: $"❌ Failed to get repositories.\n\n" +
                          $"Status: {response.StatusCode}\n" +
                          $"Please try again later.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var repos = JsonSerializer.Deserialize<JsonElement[]>(content);

            if (repos == null || repos.Length == 0)
            {
                await _telegramClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "📂 **No repositories found.**\n\n" +
                          "You don't have any repositories yet.\n" +
                          "Create your first repository on GitHub!",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            var repoMessage = "📂 **Your Recent Repositories:**\n\n";
            foreach (var repo in repos.Take(10))
            {
                var name = repo.GetProperty("full_name").GetString();
                var description = repo.TryGetProperty("description", out var desc) &&
                                  !desc.ValueKind.Equals(JsonValueKind.Null)
                    ? desc.GetString()
                    : "No description";
                var language = repo.TryGetProperty("language", out var lang) &&
                               !lang.ValueKind.Equals(JsonValueKind.Null)
                    ? lang.GetString()
                    : "Unknown";
                var stars = repo.GetProperty("stargazers_count").GetInt32();
                var forks = repo.GetProperty("forks_count").GetInt32();
                var isPrivate = repo.GetProperty("private").GetBoolean();
                var updatedAt = repo.GetProperty("updated_at").GetString();

                var lastUpdated = DateTime.TryParse(updatedAt, out var updateDate)
                    ? updateDate.ToString("yyyy-MM-dd")
                    : "Unknown";

                repoMessage += $"• **{name}** {(isPrivate ? "🔒" : "🌐")}\n" +
                               $"  📝 {description}\n" +
                               $"  💻 {language} | ⭐ {stars} | 🍴 {forks}\n" +
                               $"  📅 Updated: {lastUpdated}\n\n";
            }

            repoMessage += "💡 **Tip:** Use `/subscribe owner/repo` to get notifications for a repository!";

            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: repoMessage,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing GitHub response for user {UserId}", userId);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Error parsing GitHub response.\n\n" +
                      "Please try again later.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error for user {UserId}", userId);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Network error occurred.\n\n" +
                      "Please check your internet connection and try again.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error for user {UserId}", userId);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Unexpected error occurred.\n\n" +
                      "Please try again later or contact support.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
    }
}