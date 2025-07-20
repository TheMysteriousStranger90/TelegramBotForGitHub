using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class ClearNotificationsCommand : TextBasedCommand
{
    protected override string Pattern => "clearnotifications";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClearNotificationsCommand> _logger;

    public ClearNotificationsCommand(ITelegramBotClient telegramClient, IGitHubAuthService authService,
        HttpClient httpClient, ILogger<ClearNotificationsCommand> logger)
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

            var response = await _httpClient.PutAsync("https://api.github.com/notifications", null);

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
                    text: $"❌ Failed to clear notifications.\n\n" +
                          $"Status: {response.StatusCode}\n" +
                          $"Please try again later.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: CancellationToken.None);
                return;
            }

            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "✅ **All notifications marked as read!**\n\n" +
                      "Your GitHub notifications have been cleared.\n" +
                      "Use `/notifications` to check for new ones.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing notifications for user {UserId}", userId);
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "❌ Unexpected error occurred.\n\n" +
                      "Please try again later or contact support.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
        }
    }
}