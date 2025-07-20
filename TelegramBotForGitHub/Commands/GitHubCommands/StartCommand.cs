using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotForGitHub.Commands.Core;

namespace TelegramBotForGitHub.Commands.GitHubCommands;

public class StartCommand : TextBasedCommand
{
    protected override string Pattern => "start";
    private readonly ITelegramBotClient _telegramClient;

    public StartCommand(ITelegramBotClient telegramClient)
    {
        _telegramClient = telegramClient;
    }

    public override async Task Execute(Message message)
    {
        var welcomeMessage = $"🚀 **Welcome to GitHub Bot!**\n\n" +
                            $"Hi {message.From?.FirstName ?? "there"}! I'm your GitHub assistant.\n\n" +
                            $"**What I can do:**\n" +
                            $"• 📊 Monitor your GitHub repositories\n" +
                            $"• 🔔 Send notifications about issues, PRs, and commits\n" +
                            $"• 👤 Show your GitHub profile and repositories\n" +
                            $"• 📈 Display repository statistics\n\n" +
                            $"**Quick Start:**\n" +
                            $"1. Use /auth to authorize with GitHub\n" +
                            $"2. Use /myrepos to see your repositories\n" +
                            $"3. Use /subscribe owner/repo to get notifications\n\n" +
                            $"**Available Commands:**\n" +
                            $"• /help - Show all commands\n" +
                            $"• /auth - Authorize with GitHub\n" +
                            $"• /profile - Show your GitHub profile\n" +
                            $"• /myrepos - List your repositories\n" +
                            $"• /subscribe <repo> - Subscribe to notifications\n" +
                            $"• /repos - Show subscribed repositories\n\n" +
                            $"Let's get started! 🎉";

        await _telegramClient.SendMessage(
            chatId: message.Chat.Id,
            text: welcomeMessage,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: CancellationToken.None);
    }
}