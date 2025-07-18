using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands;

public class StartCommand : TextBasedCommand
{
    protected override string Pattern => "start";

    private readonly ITelegramBotService _telegramBotService;
    private readonly IUserSessionService _userSessionService;

    public StartCommand(ITelegramBotService telegramBotService, IUserSessionService userSessionService)
    {
        _telegramBotService = telegramBotService;
        _userSessionService = userSessionService;
    }

    public override async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        
        var session = await _userSessionService.GetUserSessionAsync(chatId);
        if (session == null)
        {
            session = await _userSessionService.CreateUserSessionAsync(chatId);
        }

        var welcomeMessage = $"🤖 <b>Welcome to GitHub Bot!</b>\n\n" +
                           $"Hello, {message.From?.FirstName ?? "User"}! 👋\n\n" +
                           $"This bot helps you work with GitHub repositories directly from Telegram.\n\n" +
                           $"<b>Available features:</b>\n" +
                           $"• 🔐 GitHub authorization\n" +
                           $"• 👤 View your GitHub profile\n" +
                           $"• 📚 Browse your repositories\n" +
                           $"• 🐛 Manage issues\n" +
                           $"• 🔄 Real-time notifications\n\n" +
                           $"<b>Available commands:</b>\n" +
                           $"/start - Show this welcome message\n" +
                           $"/auth - Authorize with GitHub\n" +
                           $"/profile - Show your GitHub profile\n" +
                           $"/repos - Show your repositories\n" +
                           $"/logout - Logout from GitHub\n" +
                           $"/help - Show help information\n\n" +
                           $"Get started by authorizing with GitHub! 🚀";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔐 Authorize with GitHub", "auth_github")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📚 Main Menu", "back_to_main")
            }
        });

        await _telegramBotService.SendMessageAsync(chatId, welcomeMessage, ParseMode.Html);
    }
}