using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotForGitHub.Services.Interfaces;

public interface ITelegramBotService
{
    Task HandleUpdateAsync(Update update);
    Task SendMessageAsync(long chatId, string message);
    Task SendMessageAsync(long chatId, string message, ParseMode parseMode);
    Task SendKeyboardAsync(long chatId, string message, List<List<string>> keyboard);
}