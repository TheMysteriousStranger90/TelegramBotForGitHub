using Telegram.Bot.Types;

namespace TelegramBotForGitHub.Services.Interfaces;

public interface ITelegramBotService
{
    Task HandleUpdateAsync(Update update);
    Task SendMessageAsync(long chatId, string message);
    Task SendKeyboardAsync(long chatId, string message, List<List<string>> keyboard);
}