using Telegram.Bot.Types;

namespace TelegramBotForGitHub.Services.Interfaces;

public interface ITelegramService
{
    Task SendNotificationAsync(long chatId, string message);
    Task<string> FormatGitHubNotificationAsync(string eventType, object eventData);
    Task HandleUpdateAsync(Message message);
}