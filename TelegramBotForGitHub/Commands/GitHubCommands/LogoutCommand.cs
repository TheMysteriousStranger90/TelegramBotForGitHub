using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class LogoutCommand : TextBasedCommand
{
    protected override string Pattern => "logout";

    private readonly ITelegramBotClient _telegramClient;
    private readonly IGitHubAuthService _authService;

    public LogoutCommand(ITelegramBotClient telegramClient, IGitHubAuthService authService)
    {
        _telegramClient = telegramClient;
        _authService = authService;
    }

    public override async Task Execute(Message message)
    {
        var userId = message.From!.Id;

        var isAuthorized = await _authService.IsUserAuthorizedAsync(userId);

        if (!isAuthorized)
        {
            await _telegramClient.SendMessage(
                chatId: message.Chat.Id,
                text: "ℹ️ You are not currently logged in to GitHub.\n\n" +
                      "Use `/auth` to authorize with GitHub.",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                cancellationToken: CancellationToken.None);
            return;
        }

        await _authService.LogoutUserAsync(userId);

        await _telegramClient.SendMessage(
            chatId: message.Chat.Id,
            text: "✅ **Successfully logged out!**\n\n" +
                  "Your GitHub authorization has been revoked.\n" +
                  "Use `/auth` to login again when you're ready.\n\n" +
                  "Thank you for using TelegramBotForGitHub! 👋",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: CancellationToken.None);
    }
}