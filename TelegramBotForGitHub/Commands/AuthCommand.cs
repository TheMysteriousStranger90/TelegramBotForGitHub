using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands;

public class AuthCommand : TextBasedCommand
{
    protected override string Pattern => "auth";

    private readonly ITelegramBotService _telegramBotService;
    private readonly IUserSessionService _userSessionService;
    private readonly IGitHubService _gitHubService;

    public AuthCommand(
        ITelegramBotService telegramBotService,
        IUserSessionService userSessionService,
        IGitHubService gitHubService)
    {
        _telegramBotService = telegramBotService;
        _userSessionService = userSessionService;
        _gitHubService = gitHubService;
    }

    public override async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        var isAuthorized = await _userSessionService.IsUserAuthorizedAsync(chatId);

        InlineKeyboardMarkup? keyboard;
        if (isAuthorized)
        {
            var session = await _userSessionService.GetUserSessionAsync(chatId);
            var responseMessage = $"✅ <b>Already authorized!</b>\n\n" +
                                $"You are already logged in as: <b>{session?.GitHubUsername ?? "GitHub User"}</b>\n\n" +
                                $"Use /logout to sign out or /profile to view your profile.";

            keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("👤 View Profile", "show_profile"),
                    InlineKeyboardButton.WithCallbackData("📚 Repositories", "show_repositories")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🚪 Logout", "logout_github")
                }
            });

            await _telegramBotService.SendMessageAsync(chatId, responseMessage, ParseMode.Html);
            return;
        }

        var authUrl = _gitHubService.GetAuthorizationUrl(chatId);
        var authMessage = $"🔐 <b>GitHub Authorization</b>\n\n" +
                         $"To use this bot, you need to authorize it with your GitHub account.\n\n" +
                         $"<b>What permissions will be granted:</b>\n" +
                         $"• 📚 Access to your repositories\n" +
                         $"• 👤 Read your profile information\n" +
                         $"• 🏢 Read organization information\n\n" +
                         $"Click the button below to start the authorization process:";

        keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithUrl("🔗 Authorize with GitHub", authUrl)
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔄 Refresh Status", "refresh_data")
            }
        });

        await _telegramBotService.SendMessageAsync(chatId, authMessage, ParseMode.Html);
    }
}