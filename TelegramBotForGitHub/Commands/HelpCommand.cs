using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotForGitHub.Commands.Core;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Commands;

public class HelpCommand : TextBasedCommand
{
    protected override string Pattern => "help";

    private readonly ITelegramBotService _telegramBotService;

    public HelpCommand(ITelegramBotService telegramBotService)
    {
        _telegramBotService = telegramBotService;
    }

    public override async Task Execute(Message message)
    {
        var helpMessage = $"📖 <b>GitHub Bot Help</b>\n\n" +
                          $"<b>📋 Available Commands:</b>\n" +
                          $"/start - Show welcome message\n" +
                          $"/auth - Authorize with GitHub\n" +
                          $"/profile - View GitHub profile\n" +
                          $"/repos - View your repositories\n" +
                          $"/issues - Manage issues\n" +
                          $"/help - Show this help message";

        await _telegramBotService.SendMessageAsync(
            message.Chat.Id,
            helpMessage,
            ParseMode.Html
        );
    }
}