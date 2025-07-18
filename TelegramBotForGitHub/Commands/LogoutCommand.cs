using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands;

public class LogoutCommand : TextBasedCommand
{
    protected override string Pattern => "logout";

    private readonly ITelegramBotService _telegramBotService;
    private readonly IUserSessionService _userSessionService;

    public LogoutCommand(ITelegramBotService telegramBotService, IUserSessionService userSessionService)
    {
        _telegramBotService = telegramBotService;
        _userSessionService = userSessionService;
    }

    public override async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        var session = await _userSessionService.GetUserSessionAsync(chatId);

        InlineKeyboardMarkup? keyboard;
        if (session?.GitHubToken == null)
        {
            var notAuthMessage = "ℹ️ <b>Not logged in</b>\n\n" +
                               "You are not currently logged in to GitHub.\n\n" +
                               "Use /auth command to authorize.";

            keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔐 Authorize", "auth_github")
                }
            });

            await _telegramBotService.SendMessageAsync(chatId, notAuthMessage, ParseMode.Html);
            return;
        }

        var username = session.GitHubUsername;
        
        session.GitHubToken = null;
        session.GitHubUsername = null;
        await _userSessionService.UpdateUserSessionAsync(session);

        var logoutMessage = $"✅ <b>Successfully logged out</b>\n\n" +
                          $"You have been logged out from GitHub account: <b>{username ?? "Unknown"}</b>\n\n" +
                          $"Your session data has been cleared. Use /auth to log in again.";

        keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔐 Authorize Again", "auth_github")
            }
        });

        await _telegramBotService.SendMessageAsync(chatId, logoutMessage, ParseMode.Html);
    }
}