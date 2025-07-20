using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class PingCommand : TextBasedCommand
{
    protected override string Pattern => "ping";

    private readonly ITelegramBotClient _telegramClient;

    public PingCommand(ITelegramBotClient telegramClient)
    {
        _telegramClient = telegramClient;
    }

    public override async Task Execute(Message message)
    {
        var startTime = DateTime.UtcNow;
        
        var pongMessage = await _telegramClient.SendMessage(
            chatId: message.Chat.Id,
            text: "🏓 Pong!",
            cancellationToken: CancellationToken.None);

        var responseTime = DateTime.UtcNow - startTime;
        
        await _telegramClient.EditMessageText(
            chatId: message.Chat.Id,
            messageId: pongMessage.MessageId,
            text: $"🏓 **Pong!**\n\n" +
                  $"**Response Time:** {responseTime.TotalMilliseconds:F0}ms\n" +
                  $"**Server Time:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                  $"**Bot Status:** ✅ Online\n\n" +
                  $"All systems are running smoothly! 🚀",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: CancellationToken.None);
    }
}