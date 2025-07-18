using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands;

public class UnhandledCommand : ICommand
{
    private readonly ITelegramBotService _telegramBotService;

    public UnhandledCommand(ITelegramBotService telegramBotService)
    {
        _telegramBotService = telegramBotService;
    }

    public bool CanExecute(Message message) => true;

    public async Task Execute(Message message)
    {
        var chatId = message.Chat.Id;
        var unknownMessage = $"❓ <b>Unknown command</b>\n\n" +
                             $"Sorry, I don't understand that command.\n\n" +
                             $"<b>Available commands:</b>\n" +
                             $"/start - Get started\n" +
                             $"/auth - Authorize with GitHub\n" +
                             $"/profile - View profile\n" +
                             $"/repos - View repositories\n" +
                             $"/logout - Log out\n" +
                             $"/help - Show help\n\n" +
                             $"Or use the menu buttons below:";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📚 Main Menu", "back_to_main"),
                InlineKeyboardButton.WithCallbackData("📖 Help", "help")
            }
        });

        await _telegramBotService.SendMessageAsync(chatId, unknownMessage, ParseMode.Html);
    }
}