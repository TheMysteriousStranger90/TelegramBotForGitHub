using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
        var welcomeMessage = new StringBuilder()
            .AppendLine("🚀 **Welcome to GitHub Bot!**")
            .AppendLine()
            .AppendLine($"Hi {message.From?.FirstName ?? "there"}! I'm your GitHub assistant.")
            .AppendLine()
            .AppendLine("**Quick Start:**")
            .AppendLine("1. Use /auth to authorize with GitHub")
            .AppendLine("2. Use /myrepos to list your repositories")
            .AppendLine("3. Use /subscribe owner/repo to subscribe to repo events")
            .AppendLine()
            .AppendLine("**Core Commands:**")
            .AppendLine("• /help - Show help message")
            .AppendLine("• /auth - Authorize with GitHub")
            .AppendLine("• /logout - Logout from GitHub")
            .AppendLine("• /profile - Show your GitHub profile")
            .AppendLine("• /myrepos - List your repositories")
            .AppendLine("• /subrepos - List subscribed repositories")
            .AppendLine("• /subscribe <repo> - Subscribe to repository notifications (e.g., microsoft/dotnet)")
            .AppendLine("• /unsubscribe <repo> - Unsubscribe from repository notifications")
            .AppendLine()
            .AppendLine("**Issue & PR Commands:**")
            .AppendLine("• /myissues - Show your open issues")
            .AppendLine("• /mypullrequests - Show your open pull requests")
            .AppendLine("• /mynotifications - Show your GitHub notifications")
            .AppendLine()
            .AppendLine("**Other User’s Info:**")
            .AppendLine("• /userrepos <username> - List another user’s repositories")
            .AppendLine("• /userissues <username> - Show a user’s open issues")
            .AppendLine("• /userpullrequests <username> - Show a user’s open pull requests")
            .AppendLine("• /usernotifications <username> - Show mentions and notifications for a user")
            .AppendLine()
            .AppendLine("**Repository Events:**")
            .AppendLine(
                "We support issues, pull requests, pushes, releases, stars, forks, and CI/CD status updates.")
            .AppendLine()
            .AppendLine("Let’s get started! 🎉")
            .ToString();

        await _telegramClient.SendMessage(
            chatId: message.Chat.Id,
            text: welcomeMessage,
            parseMode: ParseMode.Markdown,
            cancellationToken: CancellationToken.None);
    }
}